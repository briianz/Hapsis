using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Hapsis.Shared.Models;
using Hapsis.Shared.Network;

namespace Hapsis.ClientSDK
{
    public class HapsisClient
    {
        private readonly ClientWebSocket _ws = new ClientWebSocket();
        public event Action<BasePacket> OnPacketReceived;

        // 1. Csatlakozás és a háttérfigyelő elindítása
        public async Task ConnectAsync(string url)
        {
            await _ws.ConnectAsync(new Uri(url), CancellationToken.None);
            _ = ReceiveLoopAsync();
        }

        // 2. Csomag küldése a szervernek
        public async Task SendAsync(BasePacket packet)
        {
            if (_ws.State == WebSocketState.Open)
            {
                var data = PacketSerializer.Serialize(packet);
                await _ws.SendAsync(new ArraySegment<byte>(data), WebSocketMessageType.Binary, true, CancellationToken.None);
            }
        }

        // 3. Csomagok fogadása a szervertől (külön metódusban!)
        private async Task ReceiveLoopAsync()
        {
            var buffer = new byte[1024 * 4];
            while (_ws.State == WebSocketState.Open)
            {
                var result = await _ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                if (result.MessageType == WebSocketMessageType.Binary)
                {
                    var packet = PacketSerializer.Deserialize(buffer, result.Count);
                    if (packet != null)
                    {
                        OnPacketReceived?.Invoke(packet);
                    }
                }
            }
        }
    }
}