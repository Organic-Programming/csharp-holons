using Holons;

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
    }

    [Fact]
    public void DefaultUri()
    {
        Assert.Equal("tcp://:9090", Transport.DefaultUri);
    }

    [Fact]
    public void TcpListen()
    {
        var lis = Transport.Listen("tcp://127.0.0.1:0");
        try
        {
            var endpoint = (System.Net.IPEndPoint)lis.LocalEndpoint;
            Assert.True(endpoint.Port > 0);
        }
        finally
        {
            lis.Stop();
        }
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
