using System.Collections.Generic;

namespace canmarket.src.Blocks.Properties
{
    public class MarketStallProperties
    {
        public Dictionary<string, MarketStallTypeProperties> Properties;

        public string[] Types;

        public string DefaultType = "oak";

        public string VariantByGroup;

        public string VariantByGroupInventory;

        public MarketStallTypeProperties this[string type]
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
