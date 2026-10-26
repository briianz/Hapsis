using ProtoBuf;
using System;

namespace Hapsis.Shared.Models
{
    [ProtoContract]
    public class BasePacket
    {
        [ProtoMember(1)]
        public int PacketType { get; set; }

        [ProtoMember(2)]
        public string SenderId { get; set; } = string.Empty;

        [ProtoMember(3)]
        public byte[] Payload { get; set; } = Array.Empty<byte>();
    }
}