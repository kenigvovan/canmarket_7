using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;

namespace canmarket.src.Inventories.slots
{
    //This slot just ignore perish mechanics (harmony patch), because we don't want our clone slots content to spoil
    public abstract class CANNoPerishItemSlot : ItemSlot
    {
        protected CANNoPerishItemSlot(InventoryBase inventory) : base(inventory)
        {
        }
    }
}
