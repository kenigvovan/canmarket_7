using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace canmarket.src.GUI
{
    public class GUIDialogCANMarket : GuiDialogBlockEntity
    {
        public string green = "#79E02E";
        public string grey = "#855522";
        public double SSB = (GuiElementPassiveItemSlot.unscaledSlotSize);
        public double SSP = (GuiElementItemSlotGridBase.unscaledSlotPadding);
        public GUIDialogCANMarket(string dialogTitle, InventoryBase inventory, BlockPos blockEntityPos, ICoreClientAPI capi) : base(dialogTitle, inventory, blockEntityPos, capi)
        {
        }
        public virtual void SetupDialog()
        {

        }
    }
}
