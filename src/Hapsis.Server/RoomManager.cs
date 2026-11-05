using System;
using System.Collections.Concurrent;

namespace Hapsis.Server;

public class RoomManager
{
    private readonly ConcurrentDictionary<string, GameRoom> _rooms = new();
    private readonly Func<IGameLogic> _logicFactory;

    public RoomManager(Func<IGameLogic>? logicFactory = null)
    {
        _logicFactory = logicFactory ?? (() => new GameLogic());
    }

    public GameRoom GetOrCreateRoom(string roomId)
    {
        return _rooms.GetOrAdd(roomId, id => new GameRoom(id, _logicFactory()));
    }

    public bool RemoveRoom(string roomId)
    {
        return _rooms.TryRemove(roomId, out _);
    }

    public GameRoom? GetRoom(string roomId)
    {
        _rooms.TryGetValue(roomId, out var room);
        return room;
    }
}