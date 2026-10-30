using Hapsis.Shared.Network;

namespace Hapsis.Server
{
    public class PongGameLogic: IGameLogic
    {
        private float _paddle1Y = 0f;
        private float _paddle2Y = 0f;
        private float _ballX = 0f;
        private float _ballY = 0f ;
        private float _ballVelocityX = 3f;
        private float _ballVelocityY = 2f;

        public void ProcessInput(int playerId, PlayerInputMessage input)
        {
            if (playerId == 1)
            {
                _paddle1Y += Math.Clamp(_paddle1Y + input.InputY * 0.2f, -5f, 5f);
            }
            else if (playerId == 2)
            {
                _paddle2Y += Math.Clamp(_paddle2Y + input.InputY * 0.2f, -5f, 5f);
            }
        }

        public void Update(float deltaTime)
        { 
            _ballX += _ballVelocityX * deltaTime;
            _ballY += _ballVelocityY * deltaTime;

            if (_ballY is > 4.5f or < -4.5f) _ballVelocityY *= -1;

            if (_ballX is > 8f or < -8f)
            {
                _ballX = 0;
                _ballY = 0;
                _ballVelocityX *= -1;
            }
        }

        public IEnumerable<EntityState> GetEntityStates()
        {
            return new List<EntityState>
        {
            new() { EntityId = 1, PositionX = -7f, PositionY = _paddle1Y },
            new() { EntityId = 2, PositionX = 7f, PositionY = _paddle2Y },
            new() { EntityId = 3, PositionX = _ballX, PositionY = _ballY }
        };
        }
    }
}
