// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Net.Sockets;
using System.Text;
using Cratis.Stage.Host.Extensions.Packages;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Stage.Host.Extensions.Activation.given;

/// <summary>
/// A specification that starts real processes with small shell scripts for backends, which is what a tier does with a real extension. The script stands in for the extension and
/// the specification listens on the port the backend was given, because a script cannot speak gRPC.
/// </summary>
public abstract class a_process_tier : a_backend_package
{
    protected ExtensionOptions _options = null!;
    protected string _scripts = null!;
    protected List<TcpListener> _listeners = [];

    protected void Establish()
    {
        _scripts = Path.Combine(Path.GetTempPath(), "stage-tier-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_scripts);
        _options = new();
        _options.Execution.BackendsDirectory = Path.Combine(_scripts, "backends");
        _options.Execution.StartTimeout = TimeSpan.FromSeconds(20);
        _options.Execution.StopTimeout = TimeSpan.FromSeconds(10);
    }

    protected void Destroy()
    {
        foreach (var listener in _listeners) listener.Stop();
        if (Directory.Exists(_scripts)) Directory.Delete(_scripts, recursive: true);
    }

    protected IOptions<ExtensionOptions> Options => Microsoft.Extensions.Options.Options.Create(_options);

    protected ILogger<T> Logger<T>() => NullLogger<T>.Instance;

    protected string Script(string name, string body)
    {
        var path = Path.Combine(_scripts, name);
        File.WriteAllText(path, "#!/bin/sh\n" + body + "\n", new UTF8Encoding(false));
        if (OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("These specifications run shell scripts.");
        File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        return path;
    }

    protected Uri Listen()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        _listeners.Add(listener);
        return new Uri($"http://127.0.0.1:{((IPEndPoint)listener.LocalEndpoint).Port}");
    }

    protected static async Task<string> Wait(string file)
    {
        for (var attempt = 0; attempt < 200; attempt++)
        {
            if (File.Exists(file) && new FileInfo(file).Length > 0) return await File.ReadAllTextAsync(file);
            await Task.Delay(50);
        }

        return string.Empty;
    }

    protected BackendLaunch Launch(string system, LoadedExtensionPackage package, ExecutionTier tier, Uri address) => new(system, package, new(tier, package.Contract.Backend!.Runtime), address);
}
