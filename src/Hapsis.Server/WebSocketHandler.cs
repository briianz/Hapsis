using Google.Protobuf;
using Hapsis.Shared.Network;
using ProtoBuf;
using System.Net.WebSockets;


namespace Hapsis.Server
{
    public class WebSocketHandler
    {
        private const int BufferSize = 4096;

        public async Task HandleConnectionAsync(WebSocket webbSocket)
        { 
            var buffer = new byte[BufferSize];

            try {
                while (webbSocket.State == WebSocketState.Open)
                {
                    var result = await webbSocket.ReceiveAsync(
                        new ArraySegment<byte>(buffer),
                        CancellationToken.None
                        );

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await webbSocket.CloseAsync(
                            WebSocketCloseStatus.NormalClosure,
                            "Disconnected",
                            CancellationToken.None
                            );
                        break;
                    }

                    if (result.MessageType == WebSocketMessageType.Binary)
                    {
                        ProcessBinaryMessage(buffer, result.Count);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"WebSocket connection error: {ex.Message}");
            }
        }

        private void ProcessBinaryMessage(byte[] buffer, int count)
        {
            try {
                var envelope = NetworkEnvelope.Parser.ParseFrom(buffer, 0, count);
                Console.WriteLine($"Received message of type: {envelope.MessageType}");

                switch (envelope.MessageType)
                {
                    case MessageType.JoinRoomReq:
                        Console.WriteLine($" -> Csatlakozási kérelem: {envelope.JoinRoomReq.PlayerName} (Szoba: {envelope.JoinRoomReq.RoomId})");
                        break;

                    case MessageType.PlayerInput:
                        Console.WriteLine($" -> Bemenet érkezett: Player {envelope.PlayerInput.PlayerId} (Y: {envelope.PlayerInput.InputY})");
                        break;

                    default:
                        Console.WriteLine(" -> Ismeretlen üzenettípus.");
                        break;
                }
            }
            catch (InvalidProtocolBufferException ex)
            {
                Console.WriteLine($"Sérült Protobuf csomag érkezett: {ex.Message}");
            }
        }
    }
}
