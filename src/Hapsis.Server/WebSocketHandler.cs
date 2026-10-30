using System.Net.WebSockets;
using Google.Protobuf;
using Hapsis.Shared.Network;

namespace Hapsis.Server;

public class WebSocketHandler
{
    private const int BufferSize = 4096;
    private static readonly GameRoom DefaultRoom = new("default-room", new PongGameLogic());

    public async Task HandleConnectionAsync(WebSocket webSocket)
    {
        var buffer = new byte[BufferSize];
        int playerId = -1;

        try
        {
            while (webSocket.State == WebSocketState.Open)
            {
                var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Disconnected", CancellationToken.None);
                    break;
                }

                if (result.MessageType == WebSocketMessageType.Binary)
                {
                    var envelope = NetworkEnvelope.Parser.ParseFrom(buffer, 0, result.Count);

                    if (envelope.MessageType == MessageType.JoinRoomReq)
                    {
                        if (DefaultRoom.AddPlayer(webSocket, out playerId))
                        {
                            Console.WriteLine($"[HAPSIS] Player Connected! ID: {playerId}");

                            var response = new NetworkEnvelope
                            {
                                MessageType = MessageType.JoinRoomResp,
                                JoinRoomResp = new JoinRoomResponse
                                {
                                    Success = true,
                                    PlayerId = playerId,
                                    RoomId = DefaultRoom.RoomId
                                }
                            };

                            byte[] bytes = response.ToByteArray();
                            await webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Binary, true, CancellationToken.None);
                        }
                    }
                    else if (envelope.MessageType == MessageType.PlayerInput && playerId != -1)
                    {
                        DefaultRoom.RouteInput(playerId, envelope.PlayerInput);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during the management of the connection: {ex.Message}");
        }
    }
}