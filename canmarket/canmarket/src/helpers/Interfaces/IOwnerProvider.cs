using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace canmarket.src.helpers.Interfaces
{
    public interface IOwnerProvider
    {
        public string OwnerGuid { get; set; }
        public string OwnerName { get; set; }
        public void updateGuiOwner();
    }
}
