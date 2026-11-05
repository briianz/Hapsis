using System;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf;
using Hapsis.Shared.Network;

namespace Hapsis.ClientSDK;
///<summary>
/// Hapsis Framework's client-side SDK.
///This class manages the WebSocket connections, threadsafe messaging, Ping/RTT measurments and event messages
///</summary>
public class  HapsisClient
{
    #region Fields and Events
    private ClientWebSocket? _webSocket;
    private CancellationTokenSource? _cts;
    private CancellationTokenSource? _pingCts;

    //Lock to prevent concurrent WebSocket calls
    private readonly SemaphoreSlim _sendLock = new(1, 1);

    //Events forward the client application
    public event Action<NetworkEnvelope>? OnMessageReceived;
    public event Action? OnConnected;
    public event Action? OnDisconnected;

    //Framework events exposed to you, game devs:
    public event Action<NetworkEnvelope>? OnMessageReceived; // --> When a client receives a message from the server
    public event Action? OnConnected; // --> When the client successfully connects to the server
    public event Action? OnDisconnected; // --> When the client disconnects from the server
    public event Action<int>? OnPingUpdated; // --> When the ping value (latency) is updated


    ///<summary> Indicates wether the client is currently connected to the server. </summary>
    public bool IsConnected => _webSocket?.State == WebSocketState.Open;

    ///<summary> The latest measured ping value in milliseconds (round-trip). </summary>
        public int Ping { get; private set; }
    #endregion

    #region Connection and Disconnection
    ///<summary>
    /// Asynchronously connects to the Hapsis server at the specified WebSocket URL.
    ///</summary>
    public async Task ConnectAsync(string serverUrl)
    { 
        _webSocket = new ClientWebSocket();
        _cts = new CancellationTokenSource();

        try
        { 
            await _webSocket.ConnectAsync(new Uri(serverUrl), _cts.Token);
            OnConnected?.Invoke();

            //Start the background loop (message receiving and ping tracking)
            _ = ReceiveLoopAsync();

            _pingCts = new CancellationTokenSource();
            _ = StartPingLoopAsync(_pingCts.Token);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[HapsisSDK] Error connecting to server: {ex.Message}");
            OnDisconnected?.Invoke();
        }
    }

    /// <summary>
    /// Disconnects from the server and cancels active background workers.
    /// </summary>
    public async Task DisconnectAsync()
    {

        if (_webSocket != null && _webSocket.State == WebSocketState.Open)
        {
            _cts?.Cancel();
            await _webSocket.CloseAsync(
                WebSocketCloseStatus.NormalClosure,
                "Client disconnected",
                CancellationToken.None
                );
        }
    }
    #endregion

    #region Thread-safe message sending

    /// <summary>
    /// Thread-safe binary message transmission to the server using Protobuf envelope wrapping.
    /// </summary>
    public async Task SendAsync(NetworkEnvelope envelope)
    {
        if (_webSocket == null || _webSocket.State != WebSocketState.Open) return;
        
        byte[] bytes = envelope.ToByteArray();

        await _sendLock.WaitAsync();
        try {
            if (_webSocket.State == WebSocketState.Open)
            {
                await _webSocket.SendAsync(
                    new ArraySegment<byte>(bytes),
                    WebSocketMessageType.Binary,
                    true,
                    CancellationToken.None
                    );
            }
        }
        finally
        {
            _sendLock.Release();
        }
    }
    #endregion

    #region Background workers (ping & receive)

    /// <summary>
    /// [Background Worker] Sends a PingRequest once per second containing the departure timestamp.
    /// </summary>
    private async Task StartPingLoopAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));

        while (!ct.IsCancellationRequested && await timer.WaitForNextTickAsync(ct))
        {
            if (!IsConnected) break;

            long currentTimestamp = Stopwatch.GetTimestamp();
            await SendAsync(new NetworkEnvelope
            {
                MessageType = MessageType.PingReq,
                PingReq = new PingRequest { Timestamp = currentTimestamp }
            });
        }
    }

    /// <summary>
    /// [Background Worker] Listens for incoming binary WebSocket packets and dispatches them.
    /// </summary>
    private async Task ReceiveLoopAsync()
    {
        var buffer = new byte[4096];

        try {
            while (_webSocket != null && _webSocket.State == WebSocketState.Open)
            {
                var result = await _webSocket.ReceiveAsync(
                    new ArraySegment<byte>(buffer),
                    _cts?.Token ?? CancellationToken.None
                    );

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    break;
                }
                if (result.MessageType == WebSocketMessageType.Binary)
                {
                    var envelope = NetworkEnvelope.Parser.ParseFrom(buffer, 0, result.Count);
                    OnMessageReceived?.Invoke(envelope);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error receiving data: {ex.Message}");
        }
        finally
        {
            OnDisconnected?.Invoke();
        }
    }

    #endregion

}

