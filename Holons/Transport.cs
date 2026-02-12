using System;
using System.Net;
using System.Net.Sockets;

namespace Holons;

/// <summary>
/// URI-based listener factory for gRPC servers.
/// Supported: tcp://, unix://, stdio://, mem://
/// </summary>
public static class Transport
{
    /// <summary>Default transport URI when --listen is omitted.</summary>
    public const string DefaultUri = "tcp://:9090";

    /// <summary>Extract the scheme from a transport URI.</summary>
    public static string Scheme(string uri)
    {
        int idx = uri.IndexOf("://", StringComparison.Ordinal);
        return idx >= 0 ? uri[..idx] : uri;
    }

    /// <summary>Parse a transport URI and return a bound TcpListener.</summary>
    public static TcpListener Listen(string uri)
    {
        if (uri.StartsWith("tcp://"))
            return ListenTcp(uri[6..]);

        throw new ArgumentException($"unsupported transport URI: {uri}");
    }

    private static TcpListener ListenTcp(string addr)
    {
        var lastColon = addr.LastIndexOf(':');
        string host = lastColon > 0 ? addr[..lastColon] : "0.0.0.0";
        int port = int.Parse(addr[(lastColon + 1)..]);

        var listener = new TcpListener(IPAddress.Parse(host), port);
        listener.Start();
        return listener;
    }
}
