// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;

namespace Cratis.Stage.Host.for_SemanticReadModelBrowser.given;

public class a_browser_page : Specification
{
    protected HttpClient _client = null!;
    WebApplication _app = null!;

    async Task Establish()
    {
        _app = WebApplication.CreateBuilder().Build();
        _app.UsePathBase("/api/play/session");
        SemanticHost.MapReadModelBrowserPage(_app);
        _app.MapFallback(() => Results.Text("SPA fallback"));
        _app.Urls.Add("http://127.0.0.1:0");
        await _app.StartAsync();
        var addresses = _app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!;
        _client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false, CheckCertificateRevocationList = true })
        {
            BaseAddress = new Uri(addresses.Addresses.Single())
        };
    }

    async Task Destroy()
    {
        _client.Dispose();
        await _app.DisposeAsync();
    }
}
