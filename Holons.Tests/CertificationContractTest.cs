using System.Diagnostics;
using System.Text.Json.Nodes;

namespace Holons.Tests;

public class CertificationContractTest
{
    [Fact]
    public void CertJsonDeclaresLevel1ExecutablesAndDialCapabilities()
    {
        var certText = File.ReadAllText(Path.Combine(ProjectRoot(), "cert.json"));
        var cert = JsonNode.Parse(certText)?.AsObject()
            ?? throw new InvalidOperationException("cert.json must be a JSON object");

        Assert.Equal("./bin/echo-server", cert["executables"]?["echo_server"]?.GetValue<string>());
        Assert.Equal("./bin/echo-client", cert["executables"]?["echo_client"]?.GetValue<string>());
        Assert.Equal("./bin/holon-rpc-server", cert["executables"]?["holon_rpc_server"]?.GetValue<string>());
        Assert.True(cert["capabilities"]?["grpc_listen_stdio"]?.GetValue<bool>());
        Assert.True(cert["capabilities"]?["grpc_dial_tcp"]?.GetValue<bool>());
        Assert.True(cert["capabilities"]?["grpc_dial_stdio"]?.GetValue<bool>());
        Assert.True(cert["capabilities"]?["grpc_dial_ws"]?.GetValue<bool>());
        Assert.True(cert["capabilities"]?["holon_rpc_server"]?.GetValue<bool>());
    }

    [Fact]
    public void EchoWrapperScriptsExistAndAreExecutable()
    {
        var root = ProjectRoot();
        var echoClient = Path.Combine(root, "bin", "echo-client");
        var echoServer = Path.Combine(root, "bin", "echo-server");
        var holonRpcServer = Path.Combine(root, "bin", "holon-rpc-server");

        Assert.True(File.Exists(echoClient), "echo-client script is missing");
        Assert.True(File.Exists(echoServer), "echo-server script is missing");
        Assert.True(File.Exists(holonRpcServer), "holon-rpc-server script is missing");

        var clientText = File.ReadAllText(echoClient);
        var serverText = File.ReadAllText(echoServer);
        var holonRpcServerText = File.ReadAllText(holonRpcServer);
        Assert.Contains("cmd/echo-client-go/main.go", clientText);
        Assert.Contains("run ./cmd/echo-server", serverText);
        Assert.Contains("cmd/holon-rpc-server-go/main.go", holonRpcServerText);

        if (!OperatingSystem.IsWindows())
        {
            var clientMode = File.GetUnixFileMode(echoClient);
            var serverMode = File.GetUnixFileMode(echoServer);
            var holonRpcServerMode = File.GetUnixFileMode(holonRpcServer);
            Assert.True(clientMode.HasFlag(UnixFileMode.UserExecute), "echo-client is not executable");
            Assert.True(serverMode.HasFlag(UnixFileMode.UserExecute), "echo-server is not executable");
            Assert.True(holonRpcServerMode.HasFlag(UnixFileMode.UserExecute), "holon-rpc-server is not executable");
        }
    }

    [Fact]
    public async Task EchoClientWrapperInvokesGoHelperWithExpectedArguments()
    {
        if (OperatingSystem.IsWindows())
            return;

        var root = ProjectRoot();
        var tmpDir = Directory.CreateTempSubdirectory("holons-csharp-echo-wrapper-");
        var logPath = Path.Combine(tmpDir.FullName, "fake-go.log");
        var fakeGo = Path.Combine(tmpDir.FullName, "fake-go.sh");

        await File.WriteAllTextAsync(fakeGo, """
            #!/usr/bin/env bash
            set -euo pipefail
            log_file="${FAKE_GO_LOG:?}"
            {
              printf 'CWD=%s\n' "$PWD"
              i=0
              for arg in "$@"; do
                printf 'ARG%d=%s\n' "$i" "$arg"
                i=$((i+1))
              done
            } > "$log_file"
            exit 0
            """);
        File.SetUnixFileMode(
            fakeGo,
            UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = Path.Combine(root, "bin", "echo-client"),
                WorkingDirectory = root,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            }
        };
        process.StartInfo.ArgumentList.Add("stdio://");
        process.StartInfo.ArgumentList.Add("--message");
        process.StartInfo.ArgumentList.Add("cert-stdio");
        process.StartInfo.Environment["GO_BIN"] = fakeGo;
        process.StartInfo.Environment["FAKE_GO_LOG"] = logPath;

        process.Start();
        await process.WaitForExitAsync();

        Assert.Equal(0, process.ExitCode);
        Assert.True(File.Exists(logPath), "fake go invocation log missing");

        var log = await File.ReadAllTextAsync(logPath);
        Assert.Contains("CWD=", log);
        Assert.Contains("go-holons", log);
        Assert.Contains("ARG0=run", log);
        Assert.Contains("cmd/echo-client-go/main.go", log);
        Assert.Contains("--sdk", log);
        Assert.Contains("csharp-holons", log);
        Assert.Contains("--server-sdk", log);
        Assert.Contains("go-holons", log);
        Assert.Contains("stdio://", log);
        Assert.Contains("--message", log);
        Assert.Contains("cert-stdio", log);
    }

    [Fact]
    public async Task EchoServerWrapperPreservesServeArgumentOrder()
    {
        if (OperatingSystem.IsWindows())
            return;

        var root = ProjectRoot();
        var log = await RunWrapperWithFakeGo(
            Path.Combine(root, "bin", "echo-server"),
            "serve",
            "--listen",
            "stdio://");

        Assert.Contains("ARG0=run", log);
        Assert.Contains("ARG1=./cmd/echo-server", log);
        Assert.Contains("ARG2=serve", log);
        Assert.Contains("ARG3=--listen", log);
        Assert.Contains("ARG4=stdio://", log);
        Assert.Contains("ARG5=--sdk", log);
        Assert.Contains("ARG6=csharp-holons", log);
    }

    [Fact]
    public async Task HolonRpcServerWrapperForwardsSdkAndUri()
    {
        if (OperatingSystem.IsWindows())
            return;

        var root = ProjectRoot();
        var log = await RunWrapperWithFakeGo(
            Path.Combine(root, "bin", "holon-rpc-server"),
            "ws://127.0.0.1:8080/rpc",
            "--once");

        Assert.Contains("ARG0=run", log);
        Assert.Contains($"ARG1={Path.Combine(root, "cmd", "holon-rpc-server-go", "main.go")}", log);
        Assert.Contains("ARG2=--sdk", log);
        Assert.Contains("ARG3=csharp-holons", log);
        Assert.Contains("ARG4=ws://127.0.0.1:8080/rpc", log);
        Assert.Contains("ARG5=--once", log);
    }

    [Fact]
    public async Task EchoClientWrapperSupportsWsDial()
    {
        if (OperatingSystem.IsWindows())
            return;

        var root = ProjectRoot();
        var goHolonsDir = Path.Combine(root, "..", "go-holons");

        using var goServer = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = ResolveGoBinary(),
                WorkingDirectory = goHolonsDir,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            }
        };
        goServer.StartInfo.ArgumentList.Add("run");
        goServer.StartInfo.ArgumentList.Add("./cmd/echo-server");
        goServer.StartInfo.ArgumentList.Add("--listen");
        goServer.StartInfo.ArgumentList.Add("ws://127.0.0.1:0/grpc");
        goServer.StartInfo.ArgumentList.Add("--sdk");
        goServer.StartInfo.ArgumentList.Add("go-holons");

        goServer.Start();
        var wsURI = await ReadLineWithTimeout(goServer.StandardOutput, TimeSpan.FromSeconds(20));
        if (string.IsNullOrWhiteSpace(wsURI))
        {
            var stderr = await goServer.StandardError.ReadToEndAsync();
            throw new InvalidOperationException($"go ws echo-server failed to start: {stderr}");
        }

        try
        {
            using var client = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = Path.Combine(root, "bin", "echo-client"),
                    WorkingDirectory = root,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false
                }
            };
            client.StartInfo.ArgumentList.Add("--message");
            client.StartInfo.ArgumentList.Add("cert-ws");
            client.StartInfo.ArgumentList.Add("--server-sdk");
            client.StartInfo.ArgumentList.Add("go-holons");
            client.StartInfo.ArgumentList.Add(wsURI);

            client.Start();
            await client.WaitForExitAsync();

            var output = await client.StandardOutput.ReadToEndAsync();
            var error = await client.StandardError.ReadToEndAsync();

            Assert.Equal(0, client.ExitCode);
            Assert.Contains("\"status\":\"pass\"", output);
            Assert.Contains("\"response_sdk\":\"go-holons\"", output);
            Assert.True(string.IsNullOrWhiteSpace(error) || error.Contains("serve failed: EOF", StringComparison.Ordinal));
        }
        finally
        {
            try
            {
                if (!goServer.HasExited)
                    goServer.Kill(entireProcessTree: true);
            }
            catch
            {
                // ignored
            }

            await goServer.WaitForExitAsync();
        }
    }

    [Fact]
    public async Task HolonRpcServerWrapperServesEcho()
    {
        if (OperatingSystem.IsWindows())
            return;

        var root = ProjectRoot();
        using var server = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = Path.Combine(root, "bin", "holon-rpc-server"),
                WorkingDirectory = root,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            }
        };
        server.StartInfo.ArgumentList.Add("--once");

        server.Start();

        try
        {
            var url = await ReadLineWithTimeout(server.StandardOutput, TimeSpan.FromSeconds(20));
            if (string.IsNullOrWhiteSpace(url))
            {
                var stderr = await server.StandardError.ReadToEndAsync();
                throw new InvalidOperationException($"holon-rpc-server wrapper failed to start: {stderr}");
            }

            await using var client = new HolonRPCClient(
                heartbeatIntervalMs: 250,
                heartbeatTimeoutMs: 250,
                reconnectMinDelayMs: 100,
                reconnectMaxDelayMs: 400);

            await client.ConnectAsync(url);
            var result = await client.InvokeAsync(
                "echo.v1.Echo/Ping",
                new JsonObject { ["message"] = "cert-holonrpc" });

            Assert.Equal("cert-holonrpc", result["message"]?.GetValue<string>());
            Assert.Equal("csharp-holons", result["sdk"]?.GetValue<string>());
            await client.CloseAsync();

            var exitTask = server.WaitForExitAsync();
            var exited = await Task.WhenAny(exitTask, Task.Delay(TimeSpan.FromSeconds(10)));
            Assert.True(exited == exitTask, "holon-rpc-server wrapper did not exit in once mode");
            Assert.Equal(0, server.ExitCode);
        }
        finally
        {
            try
            {
                if (!server.HasExited)
                    server.Kill(entireProcessTree: true);
            }
            catch
            {
                // ignored
            }

            await server.WaitForExitAsync();
        }
    }

    private static async Task<string> RunWrapperWithFakeGo(string wrapperPath, params string[] args)
    {
        var root = ProjectRoot();
        var tmpDir = Directory.CreateTempSubdirectory("holons-csharp-wrapper-");
        var logPath = Path.Combine(tmpDir.FullName, "fake-go.log");
        var fakeGo = Path.Combine(tmpDir.FullName, "fake-go.sh");

        try
        {
            await File.WriteAllTextAsync(fakeGo, """
                #!/usr/bin/env bash
                set -euo pipefail
                log_file="${FAKE_GO_LOG:?}"
                {
                  printf 'CWD=%s\n' "$PWD"
                  i=0
                  for arg in "$@"; do
                    printf 'ARG%d=%s\n' "$i" "$arg"
                    i=$((i+1))
                  done
                } > "$log_file"
                exit 0
                """);
            if (!OperatingSystem.IsWindows())
            {
                File.SetUnixFileMode(
                    fakeGo,
                    UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            }

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = wrapperPath,
                    WorkingDirectory = root,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false
                }
            };
            foreach (var arg in args)
                process.StartInfo.ArgumentList.Add(arg);
            process.StartInfo.Environment["GO_BIN"] = fakeGo;
            process.StartInfo.Environment["FAKE_GO_LOG"] = logPath;

            process.Start();
            await process.WaitForExitAsync();

            Assert.Equal(0, process.ExitCode);
            Assert.True(File.Exists(logPath), "fake go invocation log missing");

            return await File.ReadAllTextAsync(logPath);
        }
        finally
        {
            tmpDir.Delete(recursive: true);
        }
    }

    private static async Task<string?> ReadLineWithTimeout(StreamReader reader, TimeSpan timeout)
    {
        var lineTask = reader.ReadLineAsync();
        var completed = await Task.WhenAny(lineTask, Task.Delay(timeout));
        if (completed != lineTask)
            return null;
        return await lineTask;
    }

    private static string ResolveGoBinary()
    {
        var preferred = "/Users/bpds/go/go1.25.1/bin/go";
        return File.Exists(preferred) ? preferred : "go";
    }

    private static string ProjectRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "cert.json")) &&
                Directory.Exists(Path.Combine(dir.FullName, "Holons.Tests")))
            {
                return dir.FullName;
            }
            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException("Unable to locate csharp-holons project root");
    }
}
