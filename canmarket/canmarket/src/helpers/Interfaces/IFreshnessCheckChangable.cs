using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace canmarket.src.helpers.Interfaces
{
    public interface IFreshnessCheckChangable
    {
        public float CurrentFreshnessThreshold { get; set; }
    }
}
