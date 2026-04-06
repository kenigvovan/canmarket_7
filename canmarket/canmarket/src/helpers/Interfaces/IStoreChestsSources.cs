using System.Collections.Generic;
using Vintagestory.API.MathTools;

namespace canmarket.src.helpers.Interfaces
{
    public interface IStoreChestsSources
    {
        public HashSet<Vec3i> ChestsPositions { get; set; }
    }
}
