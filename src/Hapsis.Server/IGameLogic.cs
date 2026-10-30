using Hapsis.Shared.Network;

namespace Hapsis.Server
{
    public interface IGameLogic
    {
        void ProcessInput(int playerId, PlayerInputMessage input);
        void Update(float deltaTime);
        IEnumerable<EntityState> GetEntityStates();
    }
}
