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
        private readonly ICoreClientAPI _capi;
        private readonly BlockPos _pos;
        private readonly InventoryBase _inventory;
        private readonly BEStall _be;

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

            string ownerUID = (_be as IOwnerProvider)?.OwnerGuid ?? "";
            bool isAdmin = (_be as IAdminShop).IsAdminShop;
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
            bool isAdmin = (_be as IAdminShop).IsAdminShop;
            string ownerName = (_be as IOwnerProvider)?.OwnerName ?? "";
            string title = isAdmin
                ? Lang.Get("canmarket:gui-adminshop-name")
                : Lang.Get("canmarket:gui-stall-owner", ownerName);

            ImGui.SetNextWindowSize(new Vector2(420, 540), ImGuiCond.FirstUseEver);
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
            int half = (tradeCount + 1) / 2;

            ImGuiTheme.SectionHeader(Lang.Get("canmarket:gui-stall-prices-goods"));

            Vector4 tint = _openedByOwner
                ? new Vector4(0.47f, 0.88f, 0.18f, 0.30f)
                : new Vector4(0.52f, 0.33f, 0.13f, 0.30f);

            ImGui.BeginGroup();
            for (int i = 0; i < half; i++)
                DrawTradeRow(i, tint);
            ImGui.EndGroup();

            ImGui.SameLine(0, 20);

            ImGui.BeginGroup();
            for (int i = half; i < tradeCount; i++)
                DrawTradeRow(i, tint);
            ImGui.EndGroup();
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
            string stockStr = StockString(_inventory[qtySlot].Itemstack,
                (_be as IStocksContainer).Stocks[i]);
            ImGui.TextColored(ImGuiTheme.ColorGreen, stockStr);

            if (_openedByOwner)
            {
                ImGui.SameLine(0, 6);
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + slotSize * 0.5f - 8);
                int maxS = (_be as IStocksContainer).MaxStocks[i];
                ImGui.TextDisabled("/" + (maxS == -2 ? "-" : maxS.ToString()));

                ImGui.SameLine(0, 4);
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + slotSize * 0.5f - 10);
                if (ImGui.SmallButton("...##ms"))
                {
                    _selectedRow = i;
                    _maxStockInput = maxS == -2 ? "" : maxS.ToString();
                    _maxStockPopupPending = true;
                }
            }

            ImGui.PopID();
            ImGui.Spacing();
        }

        private void DrawBooksSection()
        {
            ImGuiTheme.SectionHeader("Books");

            int ss = _slotRenderer.SlotSize;
            Vector2 p0 = ImGui.GetCursorScreenPos();

            _grid.DrawSingleSlot(0);
            ImGui.SameLine(0, 3);
            _grid.DrawSingleSlot(1);

            // Labels via DrawList — no cursor side-effects
            uint col = ImGui.GetColorU32(ImGuiTheme.ColorArrow);
            var dl = ImGui.GetWindowDrawList();
            dl.AddText(new Vector2(p0.X + 2,            p0.Y + ss + 3), col, "Warehouse");
            dl.AddText(new Vector2(p0.X + ss + 3 + 2,  p0.Y + ss + 3), col, "Log");

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
            var admin = _be as IAdminShop;
            bool infinite = admin.ProvidesInfiniteStocks;
            bool storePayment = admin.MustStorePayment;

            ImGui.Text(Lang.Get("canmarket:infinite-stocks-info-gui"));
            ImGui.SameLine();
            if (ImGui.Checkbox("##inf", ref infinite))
                _capi.Network.SendBlockEntityPacket(_pos, 1042);

            ImGui.Text(Lang.Get("canmarket:store-payment-info-gui"));
            ImGui.SameLine();
            if (ImGui.Checkbox("##pay", ref storePayment))
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
                    using var ms = new MemoryStream();
                    var w = new BinaryWriter(ms);
                    w.Write(_selectedRow);
                    w.Write(val);
                    _capi.Network.SendBlockEntityPacket(_pos, 1044, ms.ToArray());
                }
            }

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            ImGui.Text(Lang.Get("canmarket:set-item-using-text-gui", (_selectedRow + 1).ToString()));
            ImGui.Spacing();

            ImGui.Text("Slot (0/1):");
            ImGui.SameLine();
            ImGui.SetNextItemWidth(40);
            ImGui.InputInt("##shadowslot", ref _shadowSlotIndex, 0);
            _shadowSlotIndex = Math.Clamp(_shadowSlotIndex, 0, 1);

            ImGui.SameLine(0, 12);
            ImGui.Text("Stack:");
            ImGui.SameLine();
            ImGui.SetNextItemWidth(50);
            ImGui.InputText("##ss", ref _stackSizeInput, 6);

            ImGui.SetNextItemWidth(170);
            ImGui.InputText("##icode", ref _itemCodeInput, 128);
            ImGui.SameLine();
            if (ImGui.Button(Lang.Get("canmarket:gui-ok") + "##applyitem"))
            {
                var co = (CollectibleObject)_capi.World.GetItem(new AssetLocation(_itemCodeInput))
                      ?? _capi.World.GetBlock(new AssetLocation(_itemCodeInput));
                if (co != null)
                {
                    var stack = new ItemStack(co);
                    int.TryParse(_stackSizeInput, out int ss);
                    if (ss < 1) ss = 1;
                    using var ms = new MemoryStream();
                    var w = new BinaryWriter(ms);
                    w.Write(_selectedRow);
                    w.Write(_shadowSlotIndex);
                    w.Write(stack.ToBytes());
                    w.Write(ss);
                    _capi.Network.SendBlockEntityPacket(_pos, 1045, ms.ToArray());
                }
            }

            ImGui.Spacing();
            if (ImGui.Button("Close##msc", new Vector2(80, 0)))
                ImGui.CloseCurrentPopup();

            ImGui.EndPopup();
        }

        private string StockString(ItemStack stack, int amount)
        {
            if (amount == -2) return "∞";
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
