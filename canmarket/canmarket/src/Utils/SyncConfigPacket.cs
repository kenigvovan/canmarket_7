using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace canmarket.src.Utils
{
    [ProtoContract]
    public class SyncConfigPacket
    {
        [ProtoMember(1)]
        public byte[] data;
    }
}
