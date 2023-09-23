using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace canmarket.src
{
    public class Config
    {
        public static Config Current { get; set; } = new Config();
        public class Part<Config>
        {
            public readonly string Comment;
            public readonly Config Default;
            private Config val;
            public Config Val
            {
                get => (val != null ? val : val = Default);
                set => val = (value != null ? value : Default);
            }
            public Part(Config Default, string Comment = null)
            {
                this.Default = Default;
                this.Val = Default;
                this.Comment = Comment;
            }
            public Part(Config Default, string prefix, string[] allowed, string postfix = null)
            {
                this.Default = Default;
                this.Val = Default;
                this.Comment = prefix;

                this.Comment += "[" + allowed[0];
                for (int i = 1; i < allowed.Length; i++)
                {
                    this.Comment += ", " + allowed[i];
                }
                this.Comment += "]" + postfix;
            }
        }

        //ECONOMY
        public Part<string[]> IGNORED_STACK_ATTRIBTES_ARRAY = new Part<string[]>(new string[1]);

        public Part<HashSet<string>> IGNORED_STACK_ATTRIBTES_LIST = new Part<HashSet<string>>(new HashSet<string>{"candurabilitybonus"});
        public Part<float> MIN_DURABILITY_RATION = new Part<float>(0.95f);
        public Part<float> PERISH_DIVIDER = new Part<float>(2f);
        public Part<float> MESHES_RENDER_DISTANCE = new Part<float>(15);
        public Part<int> CHESTS_PER_TRADE_BLOCK = new Part<int>(6);
        public Part<int> SEARCH_CONTAINER_RADIUS = new Part<int>(3);
        public Part<int> SEARCH_WAREHOUE_DISTANCE = new Part<int>(10);
    }
}
