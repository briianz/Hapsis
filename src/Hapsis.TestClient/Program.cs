using Hapsis.ClientSDK;
using Hapsis.Shared.Models;

Console.WriteLine("Click [Enter] to connect to the server...");
Console.ReadLine();

var client = new HapsisClient();
client.OnPacketReceived += (packet) =>
{
    var message = System.Text.Encoding.UTF8.GetString(packet.Payload);
    Console.WriteLine($"Client got: {message}");
};
await client.ConnectAsync("ws://localhost:42774/ws");
Console.WriteLine("Succesfully connected to the HAPSIS server!");

var testPacket = new BasePacket
{
    PacketType = 1,
    SenderId = "TesztJatekos_01",
    Payload = new byte[] { 10, 20, 30 }
};

await client.SendAsync(testPacket);
Console.WriteLine("Tesztcsomag elküldve!");

Console.ReadLine();