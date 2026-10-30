using Hapsis.Shared.Network;

namespace Hapsis.Server;

public class PongGameLogic : IGameLogic
{
    private float _paddle1Y = 0f;
    private float _paddle2Y = 0f;
    private float _ballX = 0f;
    private float _ballY = 0f;
    private float _ballVelX = 3.5f;
    private float _ballVelY = 2.5f;

    // Ütő határai és sebessége a világban
    private const float MaxPaddleY = 3.8f;
    private const float PaddleSpeed = 0.06f;

    public void ProcessInput(int playerId, PlayerInputMessage input)
    {
        if (playerId == 1)
            _paddle1Y = Math.Clamp(_paddle1Y + input.InputY * PaddleSpeed, -MaxPaddleY, MaxPaddleY);
        else if (playerId == 2)
            _paddle2Y = Math.Clamp(_paddle2Y + input.InputY * PaddleSpeed, -MaxPaddleY, MaxPaddleY);
    }

    public void Update(float deltaTime)
    {
        _ballX += _ballVelX * deltaTime;
        _ballY += _ballVelY * deltaTime;

        // Felső/alsó falról pattanás
        if (_ballY is > 4.3f or < -4.3f)
        {
            _ballVelY *= -1;
            _ballY = Math.Clamp(_ballY, -4.3f, 4.3f);
        }

        // Bal ütővel (Entity 1, X = -7) való ütközés
        if (_ballX <= -6.8f && _ballX >= -7.2f && Math.Abs(_ballY - _paddle1Y) <= 0.8f)
        {
            _ballVelX = Math.Abs(_ballVelX);
        }

        // Jobb ütővel (Entity 2, X = 7) való ütközés
        if (_ballX >= 6.8f && _ballX <= 7.2f && Math.Abs(_ballY - _paddle2Y) <= 0.8f)
        {
            _ballVelX = -Math.Abs(_ballVelX);
        }

        // Gól esetén labda középre helyezése
        if (_ballX is > 8.5f or < -8.5f)
        {
            _ballX = 0;
            _ballY = 0;
            _ballVelX *= -1;
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