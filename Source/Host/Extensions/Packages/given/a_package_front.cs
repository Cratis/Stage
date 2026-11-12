// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Microsoft.AspNetCore.Server.Kestrel.Core;

namespace Cratis.Stage.Host.Extensions.Packages.given;

/// <summary>
/// A loaded package served over loopback, as the host serves it.
/// </summary>
public abstract class a_package_front : a_package_loader
{
    protected WebApplication _front = null!;
    protected HttpClient _client = null!;
    protected string _id = null!;

    protected async Task StartFront(bool includeRefusal = false)
    {
        Build();
        _result = await Load();
        if (includeRefusal) _result = _result with { Refusals = [new("other/extension", PackageRefusalReason.NotAllowed, "not allowed <here>")] };
        _id = _result.Loaded[0].Id;

        var builder = WebApplication.CreateEmptyBuilder(new WebApplicationOptions());
        builder.WebHost.UseKestrelCore().ConfigureKestrel(kestrel => kestrel.Listen(IPAddress.Loopback, 0, listen => listen.Protocols = HttpProtocols.Http1));
        builder.Services.AddRouting();
        _front = builder.Build();
        _front.UseRouting();
        ExtensionPackageEndpoints.MapAssets(_front, _result);
        ExtensionPackageEndpoints.MapTooling(_front, _result);
        await _front.StartAsync();
        _client = new HttpClient { BaseAddress = new Uri(_front.Urls.First()) };
    }

    protected async Task StopFront()
    {
        _client.Dispose();
        await _front.StopAsync();
        await _front.DisposeAsync();
    }

    protected Task<HttpResponseMessage> Get(string path, string? destination = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        if (destination is not null) request.Headers.Add("Sec-Fetch-Dest", destination);
        return _client.SendAsync(request);
    }
}
