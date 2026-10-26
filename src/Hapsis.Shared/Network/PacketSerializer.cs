using System.IO;
using ProtoBuf;
using Hapsis.Shared.Models;


namespace Hapsis.Shared.Network
{
    public class PacketSerializer
    {
        public static BasePacket? Deserialize(byte[] data, int length)
        { 
            using var stream = new MemoryStream(data, 0, length);
            return Serializer.Deserialize<BasePacket>(stream);
        }

        public static byte[] Serialize(BasePacket packet)
        {
            using var stream = new MemoryStream();
            Serializer.Serialize(stream, packet);
            return stream.ToArray();
        }

    }
}
