using System;
using System.Net.WebSockets;
using Google.Protobuf;
using Hapsis.Shared.Network;

namespace Hapsis.Server;

public class WebSocketHandler
{
    private const int BufferSize = 4096;
    private readonly RoomManager _roomManager;

    public WebSocketHandler(RoomManager? roomManager = null)
    {
        _roomManager = roomManager ?? new RoomManager();
    }

    public async Task HandleConnectionAsync(WebSocket webSocket)
    {
        var buffer = new byte[BufferSize];
        int playerId = -1;
        GameRoom? currentRoom = null;

        try
        {
            while (webSocket.State == WebSocketState.Open)
            {
                var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    break;
                }

                if (result.MessageType == WebSocketMessageType.Binary)
                {
                    var envelope = NetworkEnvelope.Parser.ParseFrom(buffer, 0, result.Count);

                    if (envelope.MessageType == MessageType.JoinRoomReq)
                    {
                        string requestedRoomId = string.IsNullOrEmpty(envelope.JoinRoomReq.RoomId)
                            ? "default-room"
                            : envelope.JoinRoomReq.RoomId;

                        currentRoom = _roomManager.GetOrCreateRoom(requestedRoomId);

                        if (currentRoom.AddPlayer(webSocket, out playerId))
                        {
                            Console.WriteLine($"[HAPSIS] Player Connected! ID: {playerId} | Room: {currentRoom.RoomId}");

                            var response = new NetworkEnvelope
                            {
                                MessageType = MessageType.JoinRoomResp,
                                JoinRoomResp = new JoinRoomResponse
                                {
                                    Success = true,
                                    PlayerId = playerId,
                                    RoomId = currentRoom.RoomId
                                }
                            };

                            byte[] bytes = response.ToByteArray();
                            await webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Binary, true, CancellationToken.None);
                        }
                    }
                    else if (envelope.MessageType == MessageType.PlayerInput && playerId != -1 && currentRoom != null)
                    {
                        currentRoom.RouteInput(playerId, envelope.PlayerInput);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during connection handling: {ex.Message}");
        }
        finally
        {
            if (webSocket.State != WebSocketState.Closed)
            {
                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Disconnected", CancellationToken.None);
            }
            Console.WriteLine($"[HAPSIS] Player disconnected (ID: {playerId}).");
        }
    }
}