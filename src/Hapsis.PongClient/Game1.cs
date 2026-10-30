using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Hapsis.ClientSDK;
using Hapsis.Shared.Network;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Hapsis.PongClient;

public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    private Texture2D _pixel;

    private HapsisClient _client;
    private readonly List<EntityState> _entities = new();
    private readonly object _stateLock = new();
    private bool _isSendingInput = false;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        _graphics.PreferredBackBufferWidth = 800;
        _graphics.PreferredBackBufferHeight = 600;
    }

    protected override void Initialize()
    {
        _client = new HapsisClient();
        _client.OnMessageReceived += (envelope) =>
        {
            if (envelope.MessageType == MessageType.WorldStateUpdate)
            {
                lock (_stateLock)
                {
                    _entities.Clear();
                    _entities.AddRange(envelope.WorldStateUpdate.Entities);
                }
            }
        };

        Task.Run(async () =>
        {
            await _client.ConnectAsync("ws://localhost:42774/ws");
            if (_client.IsConnected)
            {
                await _client.SendAsync(new NetworkEnvelope
                {
                    MessageType = MessageType.JoinRoomReq,
                    JoinRoomReq = new JoinRoomRequest { PlayerName = "Player", RoomId = "default-room" }
                });
            }
        });

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _pixel = new Texture2D(GraphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });
    }

    protected override void Update(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();

        if (_client.IsConnected)
        {
            var kstate = Keyboard.GetState();
            float inputY = 0;

            if (kstate.IsKeyDown(Keys.W) || kstate.IsKeyDown(Keys.Up)) inputY = -1f;
            if (kstate.IsKeyDown(Keys.S) || kstate.IsKeyDown(Keys.Down)) inputY = 1f;

            if (inputY != 0 && !_isSendingInput)
            {
                _ = SendInputAsync(inputY);
            }
        }

        base.Update(gameTime);
    }

    private async Task SendInputAsync(float inputY)
    {
        _isSendingInput = true;
        try
        {
            await _client.SendAsync(new NetworkEnvelope
            {
                MessageType = MessageType.PlayerInput,
                PlayerInput = new PlayerInputMessage { InputY = inputY }
            });
        }
        catch
        {
            // Küldési hiba elkapása
        }
        finally
        {
            _isSendingInput = false;
        }
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);
        _spriteBatch.Begin();

        float width = GraphicsDevice.Viewport.Width;
        float height = GraphicsDevice.Viewport.Height;

        lock (_stateLock)
        {
            foreach (var entity in _entities)
            {
                // Position based on Window size
                float screenX = (entity.PositionX + 8f) / 16f * width;
                float screenY = (entity.PositionY + 4.5f) / 9f * height;

                // Scale with window size
                int paddleWidth = (int)(width * 0.025f);
                int paddleHeight = (int)(height * 0.133f);
                int ballSize = (int)(height * 0.025f);

                if (entity.EntityId == 1 || entity.EntityId == 2)
                {
                    _spriteBatch.Draw(_pixel, new Rectangle((int)screenX - paddleWidth / 2, (int)screenY - paddleHeight / 2, paddleWidth, paddleHeight), Color.White);
                }
                else if (entity.EntityId == 3)
                {
                    _spriteBatch.Draw(_pixel, new Rectangle((int)screenX - ballSize / 2, (int)screenY - ballSize / 2, ballSize, ballSize), Color.Red);
                }
            }
        }

        _spriteBatch.End();
        base.Draw(gameTime);
    }
}