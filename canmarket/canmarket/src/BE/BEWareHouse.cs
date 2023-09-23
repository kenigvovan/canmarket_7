using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace canmarket.src.BE
{
    public class BEWareHouse : BlockEntityContainer
    {
        public static Random r = new Random();
        private int _key;

        public void initKey()
        {
            _key = r.Next();
        }

        public int getKey()
        {
            return _key;
        }
        public override InventoryBase Inventory => throw new NotImplementedException();

        public override string InventoryClassName => throw new NotImplementedException();
    }
}
