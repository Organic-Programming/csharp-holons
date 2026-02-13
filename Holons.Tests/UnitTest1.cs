using Holons;
using System.Net.Sockets;
using System.Text;

namespace Holons.Tests;

public class HolonsTest
{
    // --- Transport ---

    [Fact]
    public void SchemeExtraction()
    {
        Assert.Equal("tcp", Transport.Scheme("tcp://:9090"));
        Assert.Equal("unix", Transport.Scheme("unix:///tmp/x.sock"));
        Assert.Equal("stdio", Transport.Scheme("stdio://"));
        Assert.Equal("mem", Transport.Scheme("mem://"));
        Assert.Equal("ws", Transport.Scheme("ws://127.0.0.1:8080/grpc"));
        Assert.Equal("wss", Transport.Scheme("wss://example.com:443/grpc"));
    }

    [Fact]
    public void DefaultUri()
    {
        Assert.Equal("tcp://:9090", Transport.DefaultUri);
    }

    [Fact]
    public void TcpListen()
    {
        var lis = Assert.IsType<Transport.TransportListener.Tcp>(
            Transport.Listen("tcp://127.0.0.1:0"));
        try
        {
            var endpoint = (System.Net.IPEndPoint)lis.Socket.LocalEndpoint;
            Assert.True(endpoint.Port > 0);
        }
        finally
        {
            lis.Socket.Stop();
        }
    }

    [Fact]
    public void ParseUriWssDefaultPath()
    {
        var parsed = Transport.ParseUri("wss://example.com:8443");
        Assert.Equal("wss", parsed.Scheme);
        Assert.Equal("example.com", parsed.Host);
        Assert.Equal(8443, parsed.Port);
        Assert.Equal("/grpc", parsed.Path);
        Assert.True(parsed.Secure);
    }

    [Fact]
    public void StdioAndMemListenVariants()
    {
        Assert.IsType<Transport.TransportListener.Stdio>(Transport.Listen("stdio://"));
        Assert.IsType<Transport.TransportListener.Mem>(Transport.Listen("mem://"));
    }

    [Fact]
    public void UnixListenAndDialRoundTrip()
    {
        var path = Path.Combine(Path.GetTempPath(), $"holons-csharp-{Guid.NewGuid():N}.sock");
        var uri = $"unix://{path}";
        var lis = Assert.IsType<Transport.TransportListener.Unix>(Transport.Listen(uri));

        var serverTask = Task.Run(() =>
        {
            using var accepted = lis.Socket.Accept();
            var inbound = new byte[4];
            var read = 0;
            while (read < inbound.Length)
                read += accepted.Receive(inbound, read, inbound.Length - read, SocketFlags.None);
            accepted.Send(inbound, SocketFlags.None);
        });

        using var client = Transport.DialUnix(uri);
        client.Send(Encoding.UTF8.GetBytes("ping"), SocketFlags.None);

        var outbound = new byte[4];
        var got = 0;
        while (got < outbound.Length)
            got += client.Receive(outbound, got, outbound.Length - got, SocketFlags.None);
        Assert.Equal("ping", Encoding.UTF8.GetString(outbound));

        serverTask.Wait(TimeSpan.FromSeconds(3));
        lis.Socket.Dispose();
        if (File.Exists(path))
            File.Delete(path);
    }

    [Fact]
    public void MemListenAndDialRoundTrip()
    {
        var lis = Assert.IsType<Transport.TransportListener.Mem>(Transport.Listen("mem://"));

        using var client = Transport.MemDial(lis);
        using var server = lis.Runtime.Accept(1000);

        client.Socket.Send(Encoding.UTF8.GetBytes("hola"), SocketFlags.None);

        var inbound = new byte[4];
        var read = 0;
        while (read < inbound.Length)
            read += server.Socket.Receive(inbound, read, inbound.Length - read, SocketFlags.None);
        Assert.Equal("hola", Encoding.UTF8.GetString(inbound));

        server.Socket.Send(inbound, SocketFlags.None);

        var outbound = new byte[4];
        var got = 0;
        while (got < outbound.Length)
            got += client.Socket.Receive(outbound, got, outbound.Length - got, SocketFlags.None);
        Assert.Equal("hola", Encoding.UTF8.GetString(outbound));
    }

    [Fact]
    public void WsListenVariant()
    {
        var ws = Assert.IsType<Transport.TransportListener.Ws>(
            Transport.Listen("ws://127.0.0.1:8080/holon"));
        Assert.Equal("127.0.0.1", ws.Host);
        Assert.Equal(8080, ws.Port);
        Assert.Equal("/holon", ws.Path);
        Assert.False(ws.Secure);
    }

    [Fact]
    public void UnsupportedUri()
    {
        Assert.Throws<ArgumentException>(() => Transport.Listen("ftp://host"));
    }

    // --- Serve ---

    [Fact]
    public void ParseFlagsListen()
    {
        Assert.Equal("tcp://:8080",
            Serve.ParseFlags(new[] { "--listen", "tcp://:8080" }));
    }

    [Fact]
    public void ParseFlagsPort()
    {
        Assert.Equal("tcp://:3000",
            Serve.ParseFlags(new[] { "--port", "3000" }));
    }

    [Fact]
    public void ParseFlagsDefault()
    {
        Assert.Equal(Transport.DefaultUri, Serve.ParseFlags(Array.Empty<string>()));
    }

    // --- Identity ---

    [Fact]
    public void ParseHolon()
    {
        var tmpFile = Path.GetTempFileName();
        File.WriteAllText(tmpFile,
            "---\nuuid: \"abc-123\"\ngiven_name: \"test\"\n" +
            "family_name: \"Test\"\nmotto: \"A test.\"\n" +
            "clade: \"deterministic/pure\"\nlang: \"csharp\"\n" +
            "---\n# test\n");

        var id = IdentityParser.ParseHolon(tmpFile);
        Assert.Equal("abc-123", id.Uuid);
        Assert.Equal("test", id.GivenName);
        Assert.Equal("csharp", id.Lang);

        File.Delete(tmpFile);
    }

    [Fact]
    public void ParseMissingFrontmatter()
    {
        var tmpFile = Path.GetTempFileName();
        File.WriteAllText(tmpFile, "# No frontmatter\n");
        Assert.Throws<FormatException>(() => IdentityParser.ParseHolon(tmpFile));
        File.Delete(tmpFile);
    }
}
