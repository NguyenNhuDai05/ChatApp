namespace ChatClient;

public class ChatMessage
{
    public string Username { get; set; } = "";

    public string Message { get; set; } = "";

    public string Timestamp { get; set; } = "";

    public bool IsMine { get; set; }

    public bool IsSystem { get; set; }
}