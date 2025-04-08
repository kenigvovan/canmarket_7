using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace canmarket.src.GUI
{
    public class GUIDialogCANMarket : GuiDialogBlockEntity
    {
        public GUIDialogCANMarket(string dialogTitle, InventoryBase inventory, BlockPos blockEntityPos, ICoreClientAPI capi) : base(dialogTitle, inventory, blockEntityPos, capi)
        {
        }
        public void SetupDialog()
        {

        }
    }
}
