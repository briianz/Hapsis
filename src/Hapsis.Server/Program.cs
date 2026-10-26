using Hapsis.Server.Network;
using System.Net.WebSockets;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<Hapsis.Server.Managers.ConnectionManager>();
var app = builder.Build();
app.UseWebSockets();

app.Map("/ws", async (HttpContext context, Hapsis.Server.Managers.ConnectionManager connectionManager) =>
{
    if (context.WebSockets.IsWebSocketRequest)
    {
        using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
        
        var clientId = connectionManager.AddSocket(webSocket);

        Console.WriteLine($"Client connected! ID: {clientId}");

        var buffer = new byte[1024 * 4];
        var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

        while (!result.CloseStatus.HasValue)
        {
            if (result.MessageType == WebSocketMessageType.Binary)
            {
                //Nyers bájtok átalakítása BasePacketté
                var packet = Hapsis.Shared.Network.PacketSerializer.Deserialize(buffer, result.Count);

                if (packet != null)
                {
                    Console.WriteLine($"Recieved packet from {clientId}! Type: {packet.PacketType}");
                    var responsePacket = new Hapsis.Shared.Models.BasePacket
                    {
                        PacketType = 2, // Válasz csomag típusa
                        SenderId = "Server",
                        Payload = System.Text.Encoding.UTF8.GetBytes($"Server got the package, {clientId}!")
                    };

                    var responseData = Hapsis.Shared.Network.PacketSerializer.Serialize(responsePacket);
                    await connectionManager.BroadcastAsync(responseData);
                }
            }


            result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
        }

        connectionManager.RemoveSocket(clientId);
        Console.WriteLine($"Client disconnected! ID: {clientId}");
    }

    else { 
        context.Response.StatusCode = 400;
    }
});

app.Run();