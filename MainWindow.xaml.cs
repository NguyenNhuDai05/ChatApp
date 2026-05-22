using System.Collections.ObjectModel;
using System.Net.Sockets;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ChatClient;

public partial class MainWindow : Window
{
    private TcpClient? _client;

    private NetworkStream? _stream;

    private string _username = "";

    private ObservableCollection<ChatMessage>
        _messages = new();

    public MainWindow()
    {
        InitializeComponent();

        MessagesListBox.ItemsSource =
            _messages;

        DisconnectButton.IsEnabled =
            false;

        MessageTextBox.IsEnabled =
            false;
    }

    private async void ConnectButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        try
        {
            _username =
                UsernameTextBox.Text
                .Trim();

            string ip =
                IpTextBox.Text
                .Trim();

            int port =
                int.Parse(
                    PortTextBox.Text);

            _client =
                new TcpClient();

            await _client
                .ConnectAsync(
                    ip,
                    port);

            _stream =
                _client
                .GetStream();

            ConnectButton.IsEnabled =
                false;

            DisconnectButton.IsEnabled =
                true;

            MessageTextBox.IsEnabled =
                true;

            await SendRawMessage(
                $"CONNECT|{_username}");

            _ = ReceiveMessages();

            MessageTextBox.Focus();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.Message,
                "Connection Error");
        }
    }

    private async void SendButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        try
        {
            if (_stream == null)
                return;

            string text =
                MessageTextBox.Text
                .Trim();

            if (string.IsNullOrWhiteSpace(
                text))
                return;

            string message =
                $"MSG|{_username}|{text}";

            await SendRawMessage(
                message);

            MessageTextBox.Clear();

            MessageTextBox.Focus();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.Message,
                "Send Error");
        }
    }

    private async Task SendRawMessage(
        string message)
    {
        if (_stream == null)
            return;

        byte[] data =
            Encoding.UTF8
            .GetBytes(message);

        await _stream
            .WriteAsync(data);
    }

    private async Task ReceiveMessages()
    {
        try
        {
            byte[] buffer =
                new byte[4096];

            while (true)
            {
                int bytesRead =
                    await _stream!
                    .ReadAsync(buffer);

                if (bytesRead == 0)
                    break;

                string message =
                    Encoding.UTF8
                    .GetString(
                        buffer,
                        0,
                        bytesRead);

                HandleServerMessage(
                    message);
            }
        }
        catch
        {

        }
        finally
        {
            Dispatcher.Invoke(() =>
            {
                DisconnectUI();
            });
        }
    }

    private void HandleServerMessage(
        string message)
    {
        Dispatcher.Invoke(() =>
        {
            // ONLINE USERS

            if (message.StartsWith(
                "ONLINE|"))
            {
                UsersListBox.Items.Clear();

                string users =
                    message.Replace(
                        "ONLINE|",
                        "");

                string[] userList =
                    users.Split(',');

                foreach (
                    string user in userList)
                {
                    if (!string
                        .IsNullOrWhiteSpace(
                            user))
                    {
                        UsersListBox
                            .Items
                            .Add(user);
                    }
                }
            }

            // USER JOINED

            else if (message.StartsWith(
                "JOIN|"))
            {
                string username =
                    message.Replace(
                        "JOIN|",
                        "");

                AppendSystemMessage(
                    $"{username} joined the chat");
            }

            // USER LEFT

            else if (message.StartsWith(
                "LEAVE|"))
            {
                string username =
                    message.Replace(
                        "LEAVE|",
                        "");

                AppendSystemMessage(
                    $"{username} left the chat");
            }

            // NORMAL MESSAGE

            else
            {
                AppendMessage(
                    message);
            }
        });
    }

    private void AppendSystemMessage(
        string text)
    {
        _messages.Add(
            new ChatMessage
            {
                Message =
                    text,

                Timestamp =
                    DateTime.Now
                    .ToString(
                        "HH:mm"),

                IsSystem =
                    true
            });

        ScrollToBottom();
    }

    private void AppendMessage(
        string message)
    {
        int closeBracketIndex =
            message.IndexOf(']');

        if (closeBracketIndex == -1)
            return;

        string time =
            message.Substring(
                1,
                closeBracketIndex - 1);

        string remaining =
            message[
                (closeBracketIndex + 1)..]
            .Trim();

        int colonIndex =
            remaining.IndexOf(':');

        if (colonIndex == -1)
            return;

        string username =
            remaining[..colonIndex]
            .Trim();

        string text =
            remaining[
                (colonIndex + 1)..]
            .Trim();

        _messages.Add(
            new ChatMessage
            {
                Username =
                    username,

                Message =
                    text,

                Timestamp =
                    time,

                IsMine =
                    username ==
                    _username
            });

        ScrollToBottom();
    }

    private void ScrollToBottom()
    {
        if (_messages.Count == 0)
            return;

        MessagesListBox
            .ScrollIntoView(
                _messages.Last());
    }

    // =====================
    // EMOJI PICKER
    // =====================

    private void EmojiPickerButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        EmojiPopup.IsOpen =
            !EmojiPopup.IsOpen;
    }

    private void EmojiButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        Button button =
            (Button)sender;

        string emoji =
            button.Content
            .ToString()!;

        MessageTextBox.Text +=
            emoji;

        MessageTextBox.Focus();

        MessageTextBox.CaretIndex =
            MessageTextBox
            .Text
            .Length;

        EmojiPopup.IsOpen =
            false;
    }

    // =====================
    // ENTER SEND
    // =====================

    private void MessageTextBox_KeyDown(
        object sender,
        KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            SendButton_Click(
                sender,
                e);

            e.Handled =
                true;
        }
    }

    // =====================
    // DISCONNECT
    // =====================

    private async void DisconnectButton_Click(
        object sender,
        RoutedEventArgs e)
    {
        try
        {
            if (_stream != null)
            {
                byte[] data =
                    Encoding.UTF8
                    .GetBytes(
                        $"DISCONNECT|{_username}");

                await _stream
                    .WriteAsync(data);
            }
        }
        catch
        {

        }

        DisconnectUI();
    }

    private void DisconnectUI()
    {
        try
        {
            _stream?.Close();

            _client?.Close();

            _stream = null;

            _client = null;

            UsersListBox.Items.Clear();

            ConnectButton.IsEnabled =
                true;

            DisconnectButton.IsEnabled =
                false;

            MessageTextBox.IsEnabled =
                false;

            EmojiPopup.IsOpen =
                false;
        }
        catch
        {

        }
    }
}