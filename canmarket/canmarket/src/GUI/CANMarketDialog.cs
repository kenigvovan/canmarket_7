using System;
using System.Numerics;
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
    public class CANMarketDialog : IDisposable
    {
        private readonly ICoreClientAPI _capi;
        private readonly BlockPos _pos;
        private readonly InventoryCANMarketOnChest _inventory;

        private readonly ImGuiModSystem _imguiSys;
        private readonly ImGuiInventoryDialog _ghostDialog;
        private readonly ItemIconAtlas _iconAtlas;
        private readonly ImGuiSlotRenderer _slotRenderer;
        private readonly ImGuiInventoryGrid _grid;

        private readonly int _tradeCount;
        private readonly bool _openedByOwner;

        private bool _isOpen;
        public Action OnClosed;

        public CANMarketDialog(ICoreClientAPI capi, BlockPos pos, InventoryBase inventory)
        {
            _capi = capi;
            _pos = pos.Copy();
            _inventory = (InventoryCANMarketOnChest)inventory;
            _tradeCount = _inventory.Count / 2;

            string ownerName = _inventory.be?.ownerName ?? "";
            _openedByOwner = ownerName == "" || ownerName == capi.World.Player.PlayerName;

            _imguiSys = capi.ModLoader.GetModSystem<ImGuiModSystem>();
            _ghostDialog = new ImGuiInventoryDialog(capi);
            _iconAtlas = new ItemIconAtlas(capi);
            _slotRenderer = new ImGuiSlotRenderer(capi, 48);
            _grid = new ImGuiInventoryGrid(capi, _slotRenderer, _iconAtlas, SendPacket);
            _grid.SetInventory(_inventory);
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
            string ownerName = _inventory.be?.ownerName ?? "";

            ImGui.SetNextWindowSize(new Vector2(240, 0), ImGuiCond.Appearing);
            ImGui.SetNextWindowPos(ImGui.GetIO().DisplaySize * 0.5f, ImGuiCond.FirstUseEver, new Vector2(0.5f, 0.5f));
            ImGui.Begin(Lang.Get("canmarket:gui-onchesttradeblock-bar") + "###canmkt_" + _pos,
                ref open, ImGuiWindowFlags.AlwaysAutoResize);

            if (!string.IsNullOrEmpty(ownerName))
            {
                ImGui.TextColored(new Vector4(1f, 0.85f, 0.40f, 1f), ownerName);
                ImGui.Spacing();
            }

            ImGuiTheme.SectionHeader(Lang.Get("canmarket:gui-stall-prices-goods"));
            DrawTradeRows();

            if (_capi.World.Player.WorldData.CurrentGameMode == EnumGameMode.Creative)
            {
                ImGui.Spacing();
                ImGui.Separator();
                ImGui.Spacing();
                DrawCreativeToggles();
            }

            ImGui.End();
            ImGuiTheme.Pop();

            if (!open) Close();
            return _isOpen ? CallbackGUIStatus.GrabMouse : CallbackGUIStatus.Closed;
        }

        private void DrawTradeRows()
        {
            int slotSize = _slotRenderer.SlotSize;
            Vector4 priceTint = _openedByOwner
                ? new Vector4(0.47f, 0.88f, 0.18f, 0.30f)
                : new Vector4(0.52f, 0.33f, 0.13f, 0.30f);

            for (int i = 0; i < _tradeCount; i++)
            {
                int priceSlot = i * 2;
                int goodsSlot = i * 2 + 1;
                ImGui.PushID(i);

                _grid.DrawSingleSlot(priceSlot, priceTint);

                ImGui.SameLine(0, 4);
                ImGuiTheme.DrawArrow(slotSize);
                _grid.DrawSingleSlot(goodsSlot);

                ImGui.SameLine(0, 10);
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + slotSize * 0.5f - 8);
                bool infinite = _inventory.be?.InfiniteStocks ?? false;
                int stock = _inventory.stocks[i];
                string stockStr = infinite ? "∞" : stock < 999 ? stock.ToString() : "999+";
                ImGui.TextColored(ImGuiTheme.ColorGreen, stockStr);

                ImGui.PopID();
                ImGui.Spacing();
            }
        }

        private void DrawCreativeToggles()
        {
            bool infinite = _inventory.be?.InfiniteStocks ?? false;
            bool storePayment = _inventory.be?.StorePayment ?? true;

            ImGui.Text(Lang.Get("canmarket:infinite-stocks-info-gui"));
            ImGui.SameLine();
            if (ImGui.Checkbox("##inf", ref infinite))
                _capi.Network.SendBlockEntityPacket(_pos, 1042);

            ImGui.Text(Lang.Get("canmarket:store-payment-info-gui"));
            ImGui.SameLine();
            if (ImGui.Checkbox("##pay", ref storePayment))
                _capi.Network.SendBlockEntityPacket(_pos, 1043);
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
