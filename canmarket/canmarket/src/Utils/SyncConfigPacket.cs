using ProtoBuf;

namespace canmarket.src.Utils
{
    [ProtoContract]
    public class SyncConfigPacket
    {
        [ProtoMember(1)]
        public byte[] data;
    }
}
