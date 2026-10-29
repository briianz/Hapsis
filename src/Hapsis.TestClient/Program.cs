using Hapsis.ClientSDK;
using Hapsis.Shared.Network;

Console.WriteLine("Hapsis Teszt Kliens indítása...");

var client = new HapsisClient();

client.OnConnected += () => Console.WriteLine("[Client] Sikeresen csatlakozva a szerverhez!");
client.OnDisconnected += () => Console.WriteLine("[Client] Kapcsolat bontva.");
client.OnMessageReceived += (envelope) =>
{
    Console.WriteLine($"[Client] Üzenet érkezett a szervertől: {envelope.MessageType}");
};

// Csatlakozás a szerverhez
await client.ConnectAsync("ws://localhost:42774/ws");

// Teszt üzenet küldése
if (client.IsConnected)
{
    var testEnvelope = new NetworkEnvelope
    {
        MessageType = MessageType.JoinRoomReq,
        JoinRoomReq = new JoinRoomRequest
        {
            PlayerName = "TesztJatekos",
            RoomId = "room-101"
        }
    };

    await client.SendAsync(testEnvelope);
    Console.WriteLine("[Client] JoinRoomReq teszt üzenet elküldve a szervernek.");
}

Console.WriteLine("Nyomj Entert a kilépéshez...");
Console.ReadLine();

await client.DisconnectAsync();