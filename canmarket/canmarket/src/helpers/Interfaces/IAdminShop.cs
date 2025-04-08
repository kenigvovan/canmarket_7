using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace canmarket.src.helpers.Interfaces
{
    public interface IAdminShop
    {
        public bool IsAdminShop { get; set; }
        public bool MustStorePayment { get; set; }
        public bool ProvidesInfiniteStocks { get; set; }
    }
}
