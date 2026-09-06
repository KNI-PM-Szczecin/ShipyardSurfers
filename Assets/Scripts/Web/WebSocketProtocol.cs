using System;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

public static class WebSocketProtocol
{
    private const string HANDSHAKE_GUID = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
    private const byte FINAL_FRAGMENT = 0x80;
    private const byte TEXT_OPCODE = 0x1;
    private const byte BINARY_OPCODE = 0x2;
    private const byte CLOSE_OPCODE = 0x8;

    public static bool TryHandshake(NetworkStream stream, out string path)
    {
        path = "/";
        string request = ReadHttpHeaders(stream);

        Match pathMatch = Regex.Match(request, @"^GET\s+(\S+)\s+HTTP", RegexOptions.IgnoreCase);
        if (pathMatch.Success)
        {
            path = NormalizePath(pathMatch.Groups[1].Value);
        }

        Match keyMatch = Regex.Match(request, @"Sec-WebSocket-Key:\s*(\S+)", RegexOptions.IgnoreCase);
        if (!keyMatch.Success) return false;

        string acceptKey;
        using (var sha1 = SHA1.Create())
        {
            byte[] hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(keyMatch.Groups[1].Value + HANDSHAKE_GUID));
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

    public static string NormalizePath(string rawPath)
    {
        int query = rawPath.IndexOf('?');
        if (query >= 0) rawPath = rawPath.Substring(0, query);
        if (rawPath.Length > 1) rawPath = rawPath.TrimEnd('/');
        return rawPath.Length == 0 ? "/" : rawPath.ToLowerInvariant();
    }

    public static byte[] EncodeTextFrame(string text)
    {
        return EncodeFrame(TEXT_OPCODE, Encoding.UTF8.GetBytes(text));
    }

    public static byte[] EncodeBinaryFrame(byte[] payload)
    {
        return EncodeFrame(BINARY_OPCODE, payload);
    }

    public static bool IsCloseFrame(byte firstByte) => (firstByte & 0x0F) == CLOSE_OPCODE;

    private static byte[] EncodeFrame(byte opcode, byte[] payload)
    {
        int headerLength = payload.Length < 126 ? 2 : payload.Length <= ushort.MaxValue ? 4 : 10;
        var frame = new byte[headerLength + payload.Length];
        frame[0] = (byte)(FINAL_FRAGMENT | opcode);

        if (headerLength == 2)
        {
            frame[1] = (byte)payload.Length;
        }
        else if (headerLength == 4)
        {
            frame[1] = 126;
            frame[2] = (byte)(payload.Length >> 8);
            frame[3] = (byte)payload.Length;
        }
        else
        {
            frame[1] = 127;
            ulong length = (ulong)payload.Length;
            for (int i = 0; i < 8; i++)
            {
                frame[2 + i] = (byte)(length >> (8 * (7 - i)));
            }
        }

        Buffer.BlockCopy(payload, 0, frame, headerLength, payload.Length);
        return frame;
    }

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
