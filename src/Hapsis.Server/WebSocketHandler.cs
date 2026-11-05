using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf;
using Hapsis.Shared.Network;

namespace Hapsis.Server;

/// <summary>
/// Entry point for handling incoming WebSocket connections and routing network messages.
/// </summary>
public class WebSocketHandler
{
    #region Fields and Constructor

    private const int BufferSize = 4096;
    private readonly RoomManager _roomManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="WebSocketHandler"/> class with an optional <see cref="RoomManager"/>.
    /// </summary>
    /// <param name="roomManager">The room manager instance used for game room allocation.</param>
    public WebSocketHandler(RoomManager? roomManager = null)
    {
        _roomManager = roomManager ?? new RoomManager();
    }

    #endregion

    #region Connection Handling & Message Routing

    /// <summary>
    /// Handles an active WebSocket connection, reads incoming binary packets, and routes them based on their message type.
    /// </summary>
    /// <param name="webSocket">The connected WebSocket instance.</param>
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

                if (result.MessageType == WebSocketMessageType.Close) break;

                if (result.MessageType == WebSocketMessageType.Binary)
                {
                    var envelope = NetworkEnvelope.Parser.ParseFrom(buffer, 0, result.Count);

                    switch (envelope.MessageType)
                    {
                        // 1. ROOM JOINING (JOIN_ROOM_REQ)
                        case MessageType.JoinRoomReq:
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
                            break;

                        // 2. PLAYER INPUT PROCESSING (PLAYER_INPUT)
                        case MessageType.PlayerInput:
                            if (playerId != -1 && currentRoom != null)
                            {
                                currentRoom.RouteInput(playerId, envelope.PlayerInput);
                            }
                            break;

                        // 3. PING / RTT RESPONSE (PING_REQ)
                        case MessageType.PingReq:
                            if (envelope.PingReq != null)
                            {
                                var pongEnvelope = new NetworkEnvelope
                                {
                                    MessageType = MessageType.PongResp,
                                    PongResp = new PongResponse { Timestamp = envelope.PingReq.Timestamp }
                                };

                                byte[] bytes = pongEnvelope.ToByteArray();
                                await webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Binary, true, CancellationToken.None);
                            }
                            break;

                        default:
                            Console.WriteLine($"[HAPSIS] Unhandled message type: {envelope.MessageType}");
                            break;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[HAPSIS] Error during connection handling: {ex.Message}");
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

    #endregion
}