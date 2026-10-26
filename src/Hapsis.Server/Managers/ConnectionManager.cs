using System.Collections.Concurrent;
using System.Net.WebSockets;

namespace Hapsis.Server.Managers
{
    public class ConnectionManager
    {
        private readonly ConcurrentDictionary<string, WebSocket> _connections = new();

        public string AddSocket(WebSocket socket)
        { 
            var id = Guid.NewGuid().ToString();
            _connections.TryAdd(id, socket);
            return id;
        }

        public void RemoveSocket(string id)
        {
            _connections.TryRemove(id, out _);
        }

        public async Task BroadcastAsync(byte[] data)
        {
            foreach (var connection in _connections.Values)
            {
                if (connection.State == WebSocketState.Open)
                {
                    await connection.SendAsync(new ArraySegment<byte>(data), WebSocketMessageType.Binary, true, CancellationToken.None);
                }
            }
        }
    }
}
