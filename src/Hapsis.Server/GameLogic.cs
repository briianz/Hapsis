using Hapsis.Shared.Network;

namespace Hapsis.Server;

/// <summary>
/// Empty GameLogic implementation, which does nothing
/// YOU HAVE TO WRITE YOUR OWN IMPLEMENTATION FOR THE SERVER TO WORK
/// </summary> 


public class GameLogic : IGameLogic
{
    public void ProcessInput(int playerId, PlayerInputMessage input)
    {
        // TODO: Implement your game logic here
        // This is where you would handle player input
    }
    public void Update(float deltaTime)
    {
        // TODO: Implement your game logic here
        // This is where you would implement the physics and game state updates
    }
    public IEnumerable<EntityState> GetEntityStates()
    {
        // TODO: Implement your game logic here
        // This is where you would return the entity (states, positions) list

        return Enumerable.Empty<EntityState>();
    }
}