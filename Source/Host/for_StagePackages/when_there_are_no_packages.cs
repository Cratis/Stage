// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_StagePackages;

public class when_there_are_no_packages : Specification
{
    WebApplication _app = null!;
    HttpClient _client = null!;
    string _document = null!;

    async Task Because()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        _app = builder.Build();
        StagePackageEndpoints.Map(_app);
        await _app.StartAsync();

        _client = new HttpClient { BaseAddress = new Uri(_app.Urls.First()) };
        _document = await _client.GetStringAsync("/stage/packages");
    }

    async Task Destroy()
    {
        _client.Dispose();
        await _app.DisposeAsync();
    }

    [Fact] void should_say_that_no_set_was_approved() => _document.ShouldContain("\"set\":null");
    [Fact] void should_report_no_problem() => _document.ShouldContain("\"diagnostics\":[]");
}
