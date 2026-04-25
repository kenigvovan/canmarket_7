using Vintagestory.API.Client;

namespace canmarket.src.GUI
{
    /// <summary>
    /// Invisible GuiDialog that keeps DialogsOpened > 0 while an ImGui inventory grid is active.
    /// Without this, the game auto-drops items from the mouse cursor when no dialog is open.
    /// </summary>
    public class ImGuiInventoryDialog : GuiDialog
    {
        public ImGuiInventoryDialog(ICoreClientAPI capi) : base(capi) { }

        public override string ToggleKeyCombinationCode => null;
        public override bool ShouldReceiveMouseEvents() => false;
        public override bool ShouldReceiveKeyboardEvents() => false;
        public override bool ShouldReceiveRenderEvents() => false;
        public override void OnGuiOpened() { }
        public override void OnGuiClosed() { }
    }
}
