using System.Net;
using System.Net.Sockets;
using System.Text;

List<ClientHandler> clients =
    new();

Console.Title =
    "CHAT SERVER";

Console.Write(
    "IP Address: ");

string ip =
    Console.ReadLine()!;

Console.Write(
    "Port: ");

int port =
    int.Parse(
        Console.ReadLine()!);

TcpListener server =
    new TcpListener(
        IPAddress.Any,
        port);

server.Start();

Console.WriteLine();
Console.WriteLine(
    $"Server started on {ip}:{port}");

while (true)
{
    TcpClient client =
        await server
        .AcceptTcpClientAsync();

    Console.WriteLine(
        "New client connected.");

    ClientHandler handler =
        new ClientHandler(
            client,
            clients);

    clients.Add(
        handler);

    _ = handler
        .ProcessAsync();
}

public class ClientHandler
{
    private readonly TcpClient
        _client;

    private readonly List<ClientHandler>
        _clients;

    private readonly NetworkStream
        _stream;

    public string Username
    {
        get;
        set;
    } = "";

    public ClientHandler(
        TcpClient client,
        List<ClientHandler> clients)
    {
        _client =
            client;

        _clients =
            clients;

        _stream =
            client.GetStream();
    }

    public async Task ProcessAsync()
    {
        try
        {
            byte[] buffer =
                new byte[4096];

            while (true)
            {
                int bytesRead =
                    await _stream
                    .ReadAsync(buffer);

                if (bytesRead == 0)
                    break;

                string message =
                    Encoding.UTF8
                    .GetString(
                        buffer,
                        0,
                        bytesRead);

                await HandleMessage(
                    message);
            }
        }
        catch
        {

        }
        finally
        {
            Disconnect();
        }
    }

    private async Task HandleMessage(
        string message)
    {
        if (string.IsNullOrWhiteSpace(
            message))
            return;

        string[] parts =
            message.Split('|');

        if (parts.Length == 0)
            return;

        string command =
            parts[0];

        switch (command)
        {
            // ==================
            // CONNECT
            // ==================

            case "CONNECT":

                if (parts.Length < 2)
                    return;

                Username =
                    parts[1];

                Console.WriteLine(
                    $"{Username} connected.");

                await Broadcast(
                    $"JOIN|{Username}");

                await BroadcastOnlineUsers();

                break;

            // ==================
            // CHAT MESSAGE
            // ==================

            case "MSG":

                if (parts.Length < 3)
                    return;

                string user =
                    parts[1];

                string text =
                    string.Join(
                        "|",
                        parts.Skip(2));

                string finalMessage =
                    $"[{DateTime.Now:HH:mm}] {user}: {text}";

                Console.WriteLine(
                    finalMessage);

                await Broadcast(
                    finalMessage);

                break;

            // ==================
            // DISCONNECT
            // ==================

            case "DISCONNECT":

                Disconnect();

                break;
        }
    }

    private async Task Broadcast(
        string message)
    {
        byte[] data =
            Encoding.UTF8
            .GetBytes(
                message);

        foreach (
            var client
            in _clients.ToList())
        {
            try
            {
                await client
                    ._stream
                    .WriteAsync(
                        data);
            }
            catch
            {

            }
        }
    }

    private async Task BroadcastOnlineUsers()
    {
        string users =
            string.Join(
                ",",
                _clients
                .Where(x =>
                    !string.IsNullOrWhiteSpace(
                        x.Username))
                .Select(x =>
                    x.Username));

        string message =
            $"ONLINE|{users}";

        byte[] data =
            Encoding.UTF8
            .GetBytes(
                message);

        foreach (
            var client
            in _clients.ToList())
        {
            try
            {
                await client
                    ._stream
                    .WriteAsync(
                        data);
            }
            catch
            {

            }
        }
    }

    private void Disconnect()
    {
        if (!_clients.Contains(
            this))
            return;

        string username =
            Username;

        if (!string
            .IsNullOrWhiteSpace(
                username))
        {
            Console.WriteLine(
                $"{username} disconnected.");
        }

        _clients.Remove(
            this);

        try
        {
            _stream.Close();
        }
        catch
        {

        }

        try
        {
            _client.Close();
        }
        catch
        {

        }

        // UPDATE ONLINE USERS

        _ =
            BroadcastOnlineUsers();

        // LEAVE MESSAGE

        if (!string
            .IsNullOrWhiteSpace(
                username))
        {
            _ = Broadcast(
                $"LEAVE|{username}");
        }
    }
}