// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;

namespace Cratis.Stage.Host.for_SemanticReadModelBrowser.given;

public class a_refused_browser_page : Specification
{
    protected HttpClient _client = null!;
    WebApplication _app = null!;
    string _webRoot = null!;

    async Task Establish()
    {
        _webRoot = Path.GetFullPath(Path.Combine(".ai-work", "228", $"refused-browser-{Guid.NewGuid():N}"));
        Directory.CreateDirectory(Path.Combine(_webRoot, "assets"));
        await File.WriteAllTextAsync(Path.Combine(_webRoot, "index.html"), "<html>Read models</html>");
        await File.WriteAllTextAsync(Path.Combine(_webRoot, "assets", "browser.js"), "console.log('browser');");
        _app = WebApplication.CreateBuilder(new WebApplicationOptions { WebRootPath = _webRoot }).Build();
        _app.UseDefaultFiles();
        _app.UseStaticFiles(StageStaticFileOptions.Create());
        _app.UseRouting();
        SemanticHost.MapReadModelBrowserPage(_app);
        SemanticHost.MapRefused(_app, [new StageUnsupportedIssue("Plan", "model", "The model is not admitted.")]);
        _app.Urls.Add("http://127.0.0.1:0");
        await _app.StartAsync();
        var addresses = _app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!;
        _client = new HttpClient { BaseAddress = new Uri(addresses.Addresses.Single()) };
    }

    async Task Destroy()
    {
        _client.Dispose();
        await _app.DisposeAsync();
        Directory.Delete(_webRoot, recursive: true);
    }
}
