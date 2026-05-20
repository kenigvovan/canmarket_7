using System;
using System.Numerics;
using ImGuiNET;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using VSImGui;
using VSImGui.API;

namespace canmarket.src.GUI
{
    public class CANWareHouseDialog : IDisposable
    {
        private readonly ICoreClientAPI _capi;
        private readonly BlockPos _pos;
        private readonly InventoryBase _inventory;

        private readonly ImGuiModSystem _imguiSys;
        private readonly ImGuiInventoryDialog _ghostDialog;
        private readonly ItemIconAtlas _iconAtlas;
        private readonly ImGuiSlotRenderer _slotRenderer;
        private readonly ImGuiInventoryGrid _grid;

        private bool _isOpen;
        public Action OnClosed;

        public CANWareHouseDialog(ICoreClientAPI capi, BlockPos pos, InventoryBase inventory)
        {
            _capi = capi;
            _pos = pos.Copy();
            _inventory = inventory;

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
            ImGui.SetNextWindowPos(ImGui.GetIO().DisplaySize * 0.5f, ImGuiCond.FirstUseEver, new Vector2(0.5f, 0.5f));
            ImGui.Begin(Lang.Get("canmarket:gui-warehouse-title-bar") + "###canwh_" + _pos,
                ref open, ImGuiWindowFlags.AlwaysAutoResize);

            ImGui.Spacing();
            _grid.DrawSingleSlot(0);

            ImGui.SameLine(0, 16);
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 10);
            if (ImGui.Button(Lang.Get("canmarket:gui-warehouse-sign-book")))
                _capi.Network.SendBlockEntityPacket(_pos, 1042);

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();
            ImGui.TextDisabled(Lang.Get("canmarket:gui-warehouse-radius-hover",
                canmarket.config.SEARCH_CONTAINER_RADIUS));

            ImGui.End();
            ImGuiTheme.Pop();

            if (!open) Close();
            return _isOpen ? CallbackGUIStatus.GrabMouse : CallbackGUIStatus.Closed;
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
