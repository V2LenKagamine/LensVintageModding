using ProtoBuf;
using Vintagestory.API.Common.Entities;

namespace runestory
{
    [ProtoContract]
    public class CTS_SpellPacket
    {
        [ProtoMember(1)]
        public long byPlayerID;
    }
    [ProtoContract]
    public class CTS_SelectPacket
    {
        [ProtoMember(1)]
        public long byPlayerID;
        [ProtoMember(2)]
        public string spellID;
    }
    [ProtoContract]
    public class STC_BuffSync
    {
        [ProtoMember(1)]
        public string effect;
        [ProtoMember(2)]
        public float duration;
    }
    [ProtoContract]
    public class CTS_SpellsPls
    {
    }
    [ProtoContract]
    public class STC_SpellsPls
    {
        [ProtoMember(1)]
        public string[] spells;
    }
}
