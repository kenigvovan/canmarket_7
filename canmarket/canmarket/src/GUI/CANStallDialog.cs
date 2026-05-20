using System;
using System.IO;
using System.Numerics;
using canmarket.src.BE.SupportClasses;
using canmarket.src.helpers.Interfaces;
using canmarket.src.Inventories;
using ImGuiNET;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using VSImGui;
using VSImGui.API;

namespace canmarket.src.GUI
{
    public class CANStallDialog : IDisposable
    {
        private const int TradeColCount = 3;
        private const float TradeColWidth = 300f;
        private const float TradeColGap = 20f;
        private const float WindowPadX = 40f;

        private readonly ICoreClientAPI _capi;
        private readonly BlockPos _pos;
        private readonly InventoryBase _inventory;
        private readonly BEStall _be;
        private readonly IStocksContainer _stocks;
        private readonly IAdminShop _admin;
        private readonly IOwnerProvider _owner;

        private readonly ImGuiModSystem _imguiSys;
        private readonly ImGuiInventoryDialog _ghostDialog;
        private readonly ItemIconAtlas _iconAtlas;
        private readonly ImGuiSlotRenderer _slotRenderer;
        private readonly ImGuiInventoryGrid _grid;

        private bool _isOpen;
        private readonly bool _openedByOwner;

        // max stock popup
        private bool _maxStockPopupPending;
        private int _selectedRow = -1;
        private string _maxStockInput = "";
        private string _itemCodeInput = "";
        private string _stackSizeInput = "1";
        private int _shadowSlotIndex;
        private string _itemCodeError = "";

        // freshness debounce
        private int _freshnessVal;
        private long _freshnessCallbackId;

        public Action OnClosed;

        public CANStallDialog(ICoreClientAPI capi, BlockPos pos, InventoryBase inventory)
        {
            _capi = capi;
            _pos = pos.Copy();
            _inventory = inventory;
            _be = (inventory as InventoryCANStallWithMaxStocks).be;
            _stocks = _be;
            _admin = _be;
            _owner = _be;

            string ownerUID = _owner.OwnerGuid ?? "";
            bool isAdmin = _admin.IsAdminShop;
            _openedByOwner = ownerUID == "" || (ownerUID == capi.World.Player.PlayerUID && !isAdmin);
            _freshnessVal = Math.Max(1, (int)(_be.CurrentFreshnessThreshold * 100));

            _imguiSys = capi.ModLoader.GetModSystem<ImGuiModSystem>();
            _ghostDialog = new ImGuiInventoryDialog(capi);
            _iconAtlas = new ItemIconAtlas(capi);
            _slotRenderer = new ImGuiSlotRenderer(capi, 48);
            _grid = new ImGuiInventoryGrid(capi, _slotRenderer, _iconAtlas, SendPacket);
            _grid.SetInventory((InventoryBase)_inventory);
            _imguiSys.Closed += OnImGuiSystemClosed;
        }

        public void Open()
        {
            if (_isOpen) return;
            _isOpen = true;
            _imguiSys.Draw += Draw;
            _imguiSys.Show();
            _ghostDialog.TryOpen();
        }

        public void Close()
        {
            if (!_isOpen) return;
            _isOpen = false;
            _imguiSys.Draw -= Draw;
            ImGuiInventoryGrid.SuppressMouseDrop = false;
            _ghostDialog.TryClose();
            OnClosed?.Invoke();
        }

        private void OnImGuiSystemClosed() { if (_isOpen) Close(); }

        private CallbackGUIStatus Draw(float dt)
        {
            ImGuiInventoryGrid.SuppressMouseDrop = false;
            if (!_isOpen) return CallbackGUIStatus.Closed;

            ImGuiTheme.Push();

            bool open = true;
            bool isAdmin = _admin.IsAdminShop;
            string ownerName = _owner.OwnerName ?? "";
            string title = isAdmin
                ? Lang.Get("canmarket:gui-adminshop-name")
                : Lang.Get("canmarket:gui-stall-owner", ownerName);

            int tradeCount = (_inventory.Count - 2) / 3;
            int actualCols = Math.Clamp(tradeCount, 1, TradeColCount);
            float winW = TradeColWidth * actualCols + TradeColGap * (actualCols - 1) + WindowPadX;

            ImGui.SetNextWindowSize(new Vector2(winW, 540), ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowPos(ImGui.GetIO().DisplaySize * 0.5f, ImGuiCond.FirstUseEver, new Vector2(0.5f, 0.5f));
            ImGui.Begin(title + "###canstall_" + _pos, ref open);

            DrawTradeRows();

            if (_openedByOwner)
            {
                ImGui.Spacing();
                ImGui.Separator();
                ImGui.Spacing();
                DrawBooksSection();
                ImGui.Spacing();
                DrawFreshnessSlider();
            }

            if (_capi.World.Player.WorldData.CurrentGameMode == EnumGameMode.Creative)
            {
                ImGui.Spacing();
                ImGui.Separator();
                ImGui.Spacing();
                DrawCreativeToggles();
            }

            DrawMaxStockPopup();

            ImGui.End();
            ImGuiTheme.Pop();

            if (!open) Close();
            return _isOpen ? CallbackGUIStatus.GrabMouse : CallbackGUIStatus.Closed;
        }

        private void DrawTradeRows()
        {
            int tradeCount = (_inventory.Count - 2) / 3;

            ImGuiTheme.SectionHeader(Lang.Get("canmarket:gui-stall-prices-goods"));

            Vector4 tint = _openedByOwner
                ? new Vector4(0.47f, 0.88f, 0.18f, 0.30f)
                : new Vector4(0.52f, 0.33f, 0.13f, 0.30f);

            int perCol = tradeCount / TradeColCount;
            int extra  = tradeCount % TradeColCount;
            int idx = 0;
            for (int c = 0; c < TradeColCount; c++)
            {
                int rows = perCol + (c < extra ? 1 : 0);
                if (rows == 0) break;

                if (c > 0) ImGui.SameLine(0, TradeColGap);
                ImGui.BeginGroup();
                for (int r = 0; r < rows; r++)
                    DrawTradeRow(idx++, tint);
                ImGui.EndGroup();
            }
        }

        private void DrawTradeRow(int i, Vector4 tint)
        {
            int slotSize  = _slotRenderer.SlotSize;
            int priceSlot = 2 + i * 3;
            int itemSlot  = 3 + i * 3;
            int qtySlot   = 4 + i * 3;

            ImGui.PushID(i);

            _grid.DrawSingleSlot(priceSlot, tint);

            ImGui.SameLine(0, 4);
            ImGuiTheme.DrawArrow(slotSize);
            _grid.DrawSingleSlot(itemSlot);

            ImGui.SameLine(0, 3);
            _grid.DrawSingleSlot(qtySlot, tint);

            ImGui.SameLine(0, 10);
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + slotSize * 0.5f - 8);
            int stock = _stocks.Stocks[i];
            int maxS = _stocks.MaxStocks[i];
            string stockStr = StockString(_inventory[qtySlot].Itemstack, stock);
            ImGui.TextColored(ImGuiTheme.StockColor(stock, maxS, BEStall.UNLIMITED_STOCK), stockStr);

            if (_openedByOwner)
            {
                ImGui.SameLine(0, 6);
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + slotSize * 0.5f - 8);
                ImGui.TextDisabled("/" + (maxS == BEStall.UNLIMITED_STOCK ? "-" : maxS.ToString()));

                ImGui.SameLine(0, 4);
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + slotSize * 0.5f - 10);
                if (ImGui.SmallButton("...##ms"))
                {
                    _selectedRow = i;
                    _maxStockInput = maxS == BEStall.UNLIMITED_STOCK ? "" : maxS.ToString();
                    _itemCodeError = "";
                    _maxStockPopupPending = true;
                }
            }

            ImGui.PopID();
            ImGui.Spacing();
        }

        private void DrawBooksSection()
        {
            ImGuiTheme.SectionHeader(Lang.Get("canmarket:gui-stall-books-section"));

            int ss = _slotRenderer.SlotSize;
            Vector2 p0 = ImGui.GetCursorScreenPos();

            _grid.DrawSingleSlot(0);
            ImGui.SameLine(0, 3);
            _grid.DrawSingleSlot(1);

            // Labels via DrawList — no cursor side-effects
            uint col = ImGui.GetColorU32(ImGuiTheme.ColorArrow);
            var dl = ImGui.GetWindowDrawList();
            dl.AddText(new Vector2(p0.X + 2,           p0.Y + ss + 3), col, Lang.Get("canmarket:gui-warehouse-title-bar"));
            dl.AddText(new Vector2(p0.X + ss + 3 + 2,  p0.Y + ss + 3), col, Lang.Get("canmarket:gui-stall-books-log"));

            ImGui.Dummy(new Vector2(ss * 2f + 3f, ImGui.GetTextLineHeight() + 8));
        }

        private void DrawFreshnessSlider()
        {
            ImGui.Text(Lang.Get("canmarket:gui-freshness-text"));
            ImGui.SetNextItemWidth(160);
            if (ImGui.SliderInt("##fresh", ref _freshnessVal, 1, 100))
            {
                if (_freshnessCallbackId == 0)
                {
                    _freshnessCallbackId = _capi.Event.RegisterCallback(_ =>
                    {
                        using var ms = new MemoryStream();
                        var w = new BinaryWriter(ms);
                        w.Write(_freshnessVal / 100f);
                        _capi.Network.SendBlockEntityPacket(_pos, 1046, ms.ToArray());
                        _freshnessCallbackId = 0;
                    }, 2000);
                }
            }
            ImGui.SameLine(0, 8);
            //ImGui.TextColored(ImGuiTheme.ColorGold, _freshnessVal + "%");
        }

        private void DrawCreativeToggles()
        {
            bool infinite = _admin.ProvidesInfiniteStocks;
            bool storePayment = _admin.MustStorePayment;

            if (ImGui.Checkbox(Lang.Get("canmarket:infinite-stocks-info-gui") + "##inf", ref infinite))
                _capi.Network.SendBlockEntityPacket(_pos, 1042);
            ImGui.SameLine(0, 16);
            if (ImGui.Checkbox(Lang.Get("canmarket:store-payment-info-gui") + "##pay", ref storePayment))
                _capi.Network.SendBlockEntityPacket(_pos, 1043);
        }

        private void DrawMaxStockPopup()
        {
            if (_maxStockPopupPending)
            {
                ImGui.OpenPopup("##maxstock");
                _maxStockPopupPending = false;
            }

            bool popupOpen = true;
            ImGui.SetNextWindowSize(new Vector2(280, 0), ImGuiCond.Appearing);
            if (!ImGui.BeginPopupModal("##maxstock", ref popupOpen,
                    ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoTitleBar))
                return;

            ImGuiTheme.SectionHeader(
                Lang.Get("canmarket:stock-slot-set-gui", (_selectedRow + 1).ToString()));

            ImGui.SetNextItemWidth(90);
            ImGui.InputText("##maxval", ref _maxStockInput, 10);
            ImGui.SameLine();
            if (ImGui.Button(Lang.Get("canmarket:gui-ok") + "##applymax"))
            {
                if (int.TryParse(_maxStockInput, out int val) && val >= 0)
                {
                    SendMaxStockPacket(val);
                    ImGui.CloseCurrentPopup();
                }
            }
            ImGui.SameLine();
            if (ImGui.Button(Lang.Get("canmarket:gui-stall-unlimited") + "##applymaxinf"))
            {
                SendMaxStockPacket(BEStall.UNLIMITED_STOCK);
                ImGui.CloseCurrentPopup();
            }

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            ImGui.Text(Lang.Get("canmarket:set-item-using-text-gui", (_selectedRow + 1).ToString()));
            ImGui.Spacing();

            ImGui.Text(Lang.Get("canmarket:gui-stall-popup-slot"));
            ImGui.SameLine();
            ImGui.SetNextItemWidth(40);
            ImGui.InputInt("##shadowslot", ref _shadowSlotIndex, 0);
            _shadowSlotIndex = Math.Clamp(_shadowSlotIndex, 0, 1);

            ImGui.SameLine(0, 12);
            ImGui.Text(Lang.Get("canmarket:gui-stall-popup-stack"));
            ImGui.SameLine();
            ImGui.SetNextItemWidth(50);
            ImGui.InputText("##ss", ref _stackSizeInput, 6);

            ImGui.SetNextItemWidth(170);
            ImGui.InputText("##icode", ref _itemCodeInput, 128);
            ImGui.SameLine();
            if (ImGui.Button(Lang.Get("canmarket:gui-ok") + "##applyitem"))
            {
                if (TryApplyItemCode())
                    ImGui.CloseCurrentPopup();
            }

            if (!string.IsNullOrEmpty(_itemCodeError))
                ImGui.TextColored(ImGuiTheme.ColorRed, _itemCodeError);

            ImGui.Spacing();
            if (ImGui.Button(Lang.Get("canmarket:gui-close") + "##msc", new Vector2(80, 0)))
                ImGui.CloseCurrentPopup();

            ImGui.EndPopup();
        }

        private void SendMaxStockPacket(int val)
        {
            using var ms = new MemoryStream();
            var w = new BinaryWriter(ms);
            w.Write(_selectedRow);
            w.Write(val);
            _capi.Network.SendBlockEntityPacket(_pos, 1044, ms.ToArray());
        }

        private bool TryApplyItemCode()
        {
            if (string.IsNullOrWhiteSpace(_itemCodeInput))
            {
                _itemCodeError = Lang.Get("canmarket:gui-stall-invalid-item");
                return false;
            }

            CollectibleObject co;
            try
            {
                var loc = new AssetLocation(_itemCodeInput);
                co = (CollectibleObject)_capi.World.GetItem(loc) ?? _capi.World.GetBlock(loc);
            }
            catch
            {
                co = null;
            }

            if (co == null)
            {
                _itemCodeError = Lang.Get("canmarket:gui-stall-invalid-item");
                return false;
            }

            int.TryParse(_stackSizeInput, out int ss);
            if (ss < 1) ss = 1;

            var stack = new ItemStack(co);
            using var ms = new MemoryStream();
            var w = new BinaryWriter(ms);
            w.Write(_selectedRow);
            w.Write(_shadowSlotIndex);
            w.Write(stack.ToBytes());
            w.Write(ss);
            _capi.Network.SendBlockEntityPacket(_pos, 1045, ms.ToArray());
            _itemCodeError = "";
            return true;
        }

        private string StockString(ItemStack stack, int amount)
        {
            if (amount == BEStall.UNLIMITED_STOCK) return Lang.Get("canmarket:gui-stall-unlimited");
            bool liquid = stack?.Collectible?.IsLiquid() == true;
            int v = liquid ? amount / 100 : amount;
            string suffix = liquid ? "L" : "";
            return v < 999 ? v + suffix : "999+" + suffix;
        }

        private void SendPacket(object packet) =>
            _capi.Network.SendPacketClient(packet);

        public void Dispose()
        {
            if (_isOpen) Close();
            _imguiSys.Closed -= OnImGuiSystemClosed;
            _iconAtlas?.Dispose();
            _slotRenderer?.Dispose();
        }
    }
}
