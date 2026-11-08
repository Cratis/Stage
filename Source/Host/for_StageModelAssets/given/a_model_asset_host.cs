// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Sockets;
using System.Text;
using Cratis.Specifications;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;

namespace Cratis.Stage.Host.for_StageModelAssets.given;

public abstract class a_model_asset_host : Specification
{
    protected const string SecretText = "model-source-secret";
    protected const string LogoText = "<svg xmlns=\"http://www.w3.org/2000/svg\"/>";
    protected HttpClient _client = null!;
    string _root = null!;
    string _webRoot = null!;
    WebApplication _app = null!;
    Uri _address = null!;

    protected virtual bool HasAssets => true;

    protected bool SymbolicLinksSupported { get; private set; }

    async Task Establish()
    {
        _root = Path.Combine(Path.GetTempPath(), $"stage-model-assets-{Guid.NewGuid():N}");
        _webRoot = Path.Combine(_root, "web");
        Directory.CreateDirectory(_webRoot);
        await File.WriteAllTextAsync(Path.Combine(_webRoot, "index.html"), "<html>app</html>");
        var model = Path.Combine(_root, "model");
        Directory.CreateDirectory(model);
        await File.WriteAllTextAsync(Path.Combine(model, "model.play"), SecretText);
        if (HasAssets)
        {
            Directory.CreateDirectory(Path.Combine(model, "assets", "nested"));
            await File.WriteAllTextAsync(Path.Combine(model, "assets", "logo.svg"), LogoText);
            await File.WriteAllTextAsync(Path.Combine(model, "assets", "nested", "mark.svg"), LogoText);
            await File.WriteAllTextAsync(Path.Combine(model, "assets", ".hidden.svg"), LogoText);
            SymbolicLinksSupported = TryLink(Path.Combine(model, "assets", "escape.play"), Path.Combine(model, "model.play"), false)
                && TryLink(Path.Combine(model, "assets", "escape-folder"), model, true);
        }

        _app = WebApplication.CreateBuilder(new WebApplicationOptions { WebRootPath = _webRoot }).Build();
        _app.UseStaticFiles(StageStaticFileOptions.Create());
        StageModelAssets.Use(_app, Path.Combine(model, "model.play"));
        _app.Urls.Add("http://127.0.0.1:0");
        await _app.StartAsync();
        _address = new Uri(_app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single());
        _client = new HttpClient { BaseAddress = _address };
    }

    static bool TryLink(string link, string target, bool directory)
    {
        try
        {
            if (directory) Directory.CreateSymbolicLink(link, target);
            else File.CreateSymbolicLink(link, target);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or PlatformNotSupportedException)
        {
            return false;
        }
    }

    async Task Destroy()
    {
        _client.Dispose();
        await _app.DisposeAsync();
        Directory.Delete(_root, recursive: true);
    }

    // HttpClient collapses dot segments before sending, so traversal attempts are written to the socket as-is.
    protected async Task<string> Raw(string target, string method = "GET")
    {
        using var tcp = new TcpClient();
        await tcp.ConnectAsync(_address.Host, _address.Port);
        var stream = tcp.GetStream();
        var request = Encoding.ASCII.GetBytes($"{method} {target} HTTP/1.1\r\nHost: {_address.Authority}\r\nConnection: close\r\n\r\n");
        await stream.WriteAsync(request);
        using var reader = new StreamReader(stream, Encoding.UTF8);
        return await reader.ReadToEndAsync();
    }
}
