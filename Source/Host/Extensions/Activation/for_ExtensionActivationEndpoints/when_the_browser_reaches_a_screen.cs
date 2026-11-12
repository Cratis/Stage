// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Text.Json;
using Cratis.Specifications;
using Cratis.Stage.Host.Extensions.Activation.given;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Xunit;

namespace Cratis.Stage.Host.Extensions.Activation.for_ExtensionActivationEndpoints;

public class when_the_browser_reaches_a_screen : an_activator
{
    WebApplication _front = null!;
    HttpClient _client = null!;
    HttpStatusCode _status;
    JsonElement _body;
    HttpStatusCode _otherStatus;
    JsonElement _statusJson;
    string _view = string.Empty;
    BackendState _state;

    async Task Because()
    {
        await Prepare(string.Empty, [], "Mail");
        var builder = WebApplication.CreateEmptyBuilder(new WebApplicationOptions());
        builder.WebHost.UseKestrelCore().ConfigureKestrel(kestrel => kestrel.Listen(IPAddress.Loopback, 0, listen => listen.Protocols = HttpProtocols.Http1));
        builder.Services.AddSingleton(_activator);
        builder.Services.AddSingleton(new ScreenActivation(new Screens(), _activator));
        builder.Services.AddRouting();
        _front = builder.Build();
        _front.UseRouting();
        ExtensionActivationEndpoints.MapScreens(_front);
        ExtensionActivationEndpoints.MapTooling(_front);
        await _front.StartAsync();
        _client = new HttpClient { BaseAddress = new Uri(_front.Urls.First()) };

        var reached = await _client.PostAsync($"{ExtensionActivationEndpoints.ScreensRoute}/invoices/overview", null);
        _status = reached.StatusCode;
        _body = JsonElement.Parse(await reached.Content.ReadAsStringAsync());
        await Task.Delay(100);
        _state = _activator.Systems.Find("Mail")!.State;

        _otherStatus = (await _client.PostAsync($"{ExtensionActivationEndpoints.ScreensRoute}/settings", null)).StatusCode;
        _statusJson = JsonElement.Parse(await _client.GetStringAsync(ExtensionActivationEndpoints.StatusRoute));
        _view = await _client.GetStringAsync($"{ExtensionActivationEndpoints.StatusRoute}/view");
    }

    async Task Destroy()
    {
        _client.Dispose();
        await _front.StopAsync();
        await _front.DisposeAsync();
    }

    [Fact] void should_accept_the_request_without_waiting() => _status.ShouldEqual(HttpStatusCode.Accepted);
    [Fact] void should_name_the_backends_the_screen_needs() => _body.GetProperty("backends")[0].GetProperty("system").GetString().ShouldEqual("Mail");
    [Fact] void should_start_the_backend() => _state.ShouldEqual(BackendState.Active);
    [Fact] void should_accept_a_screen_that_places_nothing_and_start_nothing() => (_otherStatus, _runtime.Starts.Count).ShouldEqual((HttpStatusCode.Accepted, 1));
    [Fact] void should_report_the_tier_state_and_policy_of_each_backend() => (
        _statusJson.GetProperty("backends")[0].GetProperty("tier").GetString(),
        _statusJson.GetProperty("backends")[0].GetProperty("state").GetString(),
        _statusJson.GetProperty("backends")[0].GetProperty("activation").GetString()).ShouldEqual(("SharedHost", "Active", "on demand, idle after 00:10:00"));
    [Fact] void should_show_the_status_as_a_page() => _view.ShouldContain("<td>SharedHost</td><td>Active</td>");

    sealed class Screens : IScreenExtensionUsage
    {
        public IReadOnlyList<string> SystemsOn(string screen) => screen == "/invoices/overview" ? ["Mail"] : [];
    }
}
