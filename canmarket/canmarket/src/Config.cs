using System.Collections.Generic;
using ProtoBuf;

namespace canmarket.src
{
    [ProtoContract]
    public class Config
    {
        [ProtoIgnore]
        public string[] IGNORED_STACK_ATTRIBTES_ARRAY;
        [ProtoIgnore]
        public HashSet<string> IGNORED_STACK_ATTRIBTES_LIST = new HashSet<string>(new HashSet<string> { "candurabilitybonus" });
        [ProtoIgnore]
        public float MIN_DURABILITY_RATION = 0.95f;

        //won't be used
        [ProtoMember(1)]
        public float PERISH_DIVIDER = 2f;
        [ProtoIgnore]
        public float MESHES_RENDER_DISTANCE = 15;
        [ProtoMember(2)]
        public int CHESTS_PER_TRADE_BLOCK = 6;
        [ProtoMember(3)]
        public int SEARCH_CONTAINER_RADIUS = 3;
        [ProtoMember(4)]
        public int SEARCH_WAREHOUE_DISTANCE = 10;
        [ProtoIgnore]
        public HashSet<string> WAREHOUSE_ITEMSTACK_NOT_IGNORED_ATTRIBUTES = new HashSet<string>(new HashSet<string> { "material", "lining", "glass", "type" });
        [ProtoIgnore]
        public bool SAVE_SLOTS_ONCHESTTRADEBLOCK = true;
        [ProtoIgnore]
        public bool SAVE_SLOTS_STALL = true;
        [ProtoMember(5)]
        public float DEFAULT_MIN_FRESHNESS_FOR_SALE_PERCENTS = 0.4f;
    }
}
