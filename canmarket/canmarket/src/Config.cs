using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace canmarket.src
{
    public class Config
    {
        public string[] IGNORED_STACK_ATTRIBTES_ARRAY;

        public HashSet<string> IGNORED_STACK_ATTRIBTES_LIST = new HashSet<string>(new HashSet<string> { "candurabilitybonus" });
        public float MIN_DURABILITY_RATION = 0.95f;
        public float PERISH_DIVIDER = 2f;
        public float MESHES_RENDER_DISTANCE = 15;
        public int CHESTS_PER_TRADE_BLOCK = 6;
        public int SEARCH_CONTAINER_RADIUS = 3;
        public int SEARCH_WAREHOUE_DISTANCE = 10;
        public HashSet<string> WAREHOUSE_ITEMSTACK_NOT_IGNORED_ATTRIBUTES = new HashSet<string>(new HashSet<string> { "material", "lining", "glass" });
        public bool SAVE_SLOTS_ONCHESTTRADEBLOCK = true;
        public bool SAVE_SLOTS_STALL = true;
    }
}
