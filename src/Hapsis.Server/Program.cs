using Hapsis.Server;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.UseWebSockets();

var webSocketHandler = new WebSocketHandler();

app.Map("/ws", async context =>
{
    if (context.WebSockets.IsWebSocketRequest)
    { 
        using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
        await webSocketHandler.HandleConnectionAsync(webSocket);
    }
    else
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
    }
});

Console.WriteLine("Hapsis server is running on address: ws://localhost:42774/ws");
app.Run("http://localhost:42774");