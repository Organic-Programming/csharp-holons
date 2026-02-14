using System.Diagnostics;

namespace Holons.Tests;

public class CertificationContractTest
{
    [Fact]
    public void CertJsonDeclaresLevel1ExecutablesAndDialCapabilities()
    {
        var cert = File.ReadAllText(Path.Combine(ProjectRoot(), "cert.json"));

        Assert.Contains("\"echo_server\": \"./bin/echo-server\"", cert);
        Assert.Contains("\"echo_client\": \"./bin/echo-client\"", cert);
        Assert.Contains("\"grpc_dial_tcp\": true", cert);
        Assert.Contains("\"grpc_dial_stdio\": true", cert);
    }

    [Fact]
    public void EchoWrapperScriptsExistAndAreExecutable()
    {
        var root = ProjectRoot();
        var echoClient = Path.Combine(root, "bin", "echo-client");
        var echoServer = Path.Combine(root, "bin", "echo-server");

        Assert.True(File.Exists(echoClient), "echo-client script is missing");
        Assert.True(File.Exists(echoServer), "echo-server script is missing");

        var clientText = File.ReadAllText(echoClient);
        var serverText = File.ReadAllText(echoServer);
        Assert.Contains("cmd/echo-client-go/main.go", clientText);
        Assert.Contains("run ./cmd/echo-server", serverText);

        if (!OperatingSystem.IsWindows())
        {
            var clientMode = File.GetUnixFileMode(echoClient);
            var serverMode = File.GetUnixFileMode(echoServer);
            Assert.True(clientMode.HasFlag(UnixFileMode.UserExecute), "echo-client is not executable");
            Assert.True(serverMode.HasFlag(UnixFileMode.UserExecute), "echo-server is not executable");
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
