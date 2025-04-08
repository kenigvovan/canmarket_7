using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace canmarket.src.helpers.Interfaces
{
    public interface IStocksContainer
    {
        public int[] Stocks { get; set; }
        public int[] MaxStocks { get; set; }
    }
}
