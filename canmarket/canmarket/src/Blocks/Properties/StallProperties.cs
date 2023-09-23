using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.GameContent;

namespace canmarket.src.Blocks.Properties
{
    public class StallProperties
    {
        public Dictionary<string, StallTypeProperties> Properties;

        public string[] Types;

        public string DefaultType = "rusty";

        public string VariantByGroup;

        public string VariantByGroupInventory;

        public StallTypeProperties this[string type]
        {
            get
            {
                if (!Properties.TryGetValue(type, out var value))
                {
                    return Properties["*"];
                }

                return value;
            }
        }
    }
}

