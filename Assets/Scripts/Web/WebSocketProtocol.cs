using System;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

public static class WebSocketProtocol
{
    private const string HandshakeGuid = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";

    public static bool TryHandshake(NetworkStream stream)
    {
        string request = ReadHttpHeaders(stream);
        Match keyMatch = Regex.Match(request, @"Sec-WebSocket-Key:\s*(\S+)", RegexOptions.IgnoreCase);
        if (!keyMatch.Success) return false;

        string acceptKey;
        using (var sha1 = SHA1.Create())
        {
            byte[] hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(keyMatch.Groups[1].Value + HandshakeGuid));
            acceptKey = Convert.ToBase64String(hash);
        }

        byte[] response = Encoding.UTF8.GetBytes(
            "HTTP/1.1 101 Switching Protocols\r\n" +
            "Upgrade: websocket\r\n" +
            "Connection: Upgrade\r\n" +
            $"Sec-WebSocket-Accept: {acceptKey}\r\n\r\n");
        stream.Write(response, 0, response.Length);
        return true;
    }

    public static byte[] EncodeTextFrame(string text)
    {
        byte[] payload = Encoding.UTF8.GetBytes(text);
        byte[] frame;

        if (payload.Length < 126)
        {
            frame = new byte[2 + payload.Length];
            frame[1] = (byte)payload.Length;
        }
        else
        {
            frame = new byte[4 + payload.Length];
            frame[1] = 126;
            frame[2] = (byte)(payload.Length >> 8);
            frame[3] = (byte)payload.Length;
        }

        frame[0] = 0x81;
        Buffer.BlockCopy(payload, 0, frame, frame.Length - payload.Length, payload.Length);
        return frame;
    }

    public static bool IsCloseFrame(byte firstByte) => (firstByte & 0x0F) == 0x8;

    private static string ReadHttpHeaders(NetworkStream stream)
    {
        var sb = new StringBuilder();
        var buf = new byte[1];
        while (sb.Length < 8192)
        {
            if (stream.Read(buf, 0, 1) <= 0) break;
            sb.Append((char)buf[0]);
            if (sb.Length >= 4 && sb.ToString(sb.Length - 4, 4) == "\r\n\r\n") break;
        }
        return sb.ToString();
    }
}
