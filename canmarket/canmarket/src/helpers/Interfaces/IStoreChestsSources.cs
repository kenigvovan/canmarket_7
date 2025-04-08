using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.MathTools;

namespace canmarket.src.helpers.Interfaces
{
    public interface IStoreChestsSources
    {
        public HashSet<Vec3i> ChestsPositions { get; set; }
    }
}
