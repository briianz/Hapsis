using System.Net.WebSockets;
using Google.Protobuf;
using Hapsis.Shared.Network;

namespace Hapsis.Server;

public class GameRoom
{
    public string RoomId { get; }
    private readonly List<WebSocket> _clients = new();
    private readonly object _lock = new();
    private readonly IGameLogic _gameLogic;

    private ulong _sequenceNumber = 0;
    private bool _isRunning = false;

    public GameRoom(string roomId, IGameLogic gameLogic)
    {
        RoomId = roomId;
        _gameLogic = gameLogic;
    }

    public bool AddPlayer(WebSocket socket, out int assignedPlayerId)
    {
        lock (_lock)
        {
            if (_clients.Count >= 2)
            {
                assignedPlayerId = -1;
                return false;
            }

            _clients.Add(socket);
            assignedPlayerId = _clients.Count;

            if (_clients.Count == 2 && !_isRunning)
            {
                StartGameLoop();
            }

            return true;
        }
    }

    public void RouteInput(int playerId, PlayerInputMessage input)
    {
        lock (_lock)
        {
            _gameLogic.ProcessInput(playerId, input);
        }
    }

    private void StartGameLoop()
    {
        _isRunning = true;
        Task.Run(async () =>
        {
            var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(16));

            while (_isRunning && await timer.WaitForNextTickAsync())
            {
                lock (_lock)
                {
                    _gameLogic.Update(0.016f); // 60 FPS fix deltaTime
                }
                await BroadcastWorldStateAsync();
            }
        });
    }

    private async Task BroadcastWorldStateAsync()
    {
        NetworkEnvelope envelope;

        lock (_lock)
        {
            _sequenceNumber++;
            var updateMsg = new WorldStateUpdateMessage { SequenceNumber = _sequenceNumber };
            updateMsg.Entities.AddRange(_gameLogic.GetEntityStates());

            envelope = new NetworkEnvelope
            {
                MessageType = MessageType.WorldStateUpdate,
                WorldStateUpdate = updateMsg
            };
        }

        byte[] bytes = envelope.ToByteArray();
        List<WebSocket> currentClients;

        lock (_lock)
        {
            currentClients = _clients.ToList();
        }

        foreach (var client in currentClients)
        {
            if (client.State == WebSocketState.Open)
            {
                try
                {
                    await client.SendAsync(
                        new ArraySegment<byte>(bytes),
                        WebSocketMessageType.Binary,
                        true,
                        CancellationToken.None
                    );
                }
                catch { }
            }
        }
    }
}