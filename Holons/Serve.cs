namespace Holons;

/// <summary>Standard gRPC server runner utilities.</summary>
public static class Serve
{
    /// <summary>Parse --listen or --port from command-line args.</summary>
    public static string ParseFlags(string[] args)
    {
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--listen" && i + 1 < args.Length)
                return args[i + 1];
            if (args[i] == "--port" && i + 1 < args.Length)
                return $"tcp://:{args[i + 1]}";
        }
        return Transport.DefaultUri;
    }
}
