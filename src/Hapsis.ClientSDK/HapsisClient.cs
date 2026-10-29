using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf;
using Hapsis.Shared.Network;

public class  HapsisClient
{

    private ClientWebSocket? _webSocket;
    private CancellationTokenSource? _cts;

    //Events forward the client application
    public event Action<NetworkEnvelope>? OnMessageReceived;
    public event Action? OnConnected;
    public event Action? OnDisconnected;

    public bool IsConnected => _webSocket?.State == WebSocketState.Open;

    public async Task ConnectAsync(string serverUrl)
    { 
        _webSocket = new ClientWebSocket();
        _cts = new CancellationTokenSource();

        try
        { 
            await _webSocket.ConnectAsync(new Uri(serverUrl), _cts.Token);
            OnConnected?.Invoke();

            //Start the background receive loop
            _ = ReceiveLoopAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error connecting to server: {ex.Message}");
            OnDisconnected?.Invoke();
        }
    }

    public async Task SendAsync(NetworkEnvelope envelope)
    {
        if (_webSocket == null || _webSocket.State != WebSocketState.Open) return;
        
        byte[] bytes = envelope.ToByteArray();
       
        await _webSocket.SendAsync(
            new ArraySegment<byte>(bytes),
            WebSocketMessageType.Binary,
            true,
            CancellationToken.None
            );
    }

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

}

