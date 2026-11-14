// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_GeneratedHostAuthentication.given;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_GeneratedHostAuthentication;

public class when_rejecting_invalid_configuration : Specification
{
    readonly List<bool> _rejected = [];
    readonly List<bool> _safeDiagnostics = [];

    void Because()
    {
        (string Key, string? Value)[] cases =
        [
            ("EntraId:Enabled", "not-a-boolean"),
            ("EntraId:TenantId", null),
            ("EntraId:TenantId", "common"),
            ("EntraId:ClientId", null),
            ("EntraId:Audience", null),
            ("PublicOrigin", "http://app.example.test"),
            ("PublicOrigin", "https://app.example.test/path"),
            ("PublicOrigin", "https://user:synthetic-invalid-value@app.example.test"),
            ("PublicOrigin", "https://app.example.test?query"),
            ("EntraId:ClientSecret", "synthetic-invalid-value"),
            ("EntraId:ClientCertificates:0:SourceType", "KeyVault"),
            ("EntraId:ClientCredentials:0:SourceType", null),
            ("EntraId:ClientCredentials:0:SourceType", "ClientSecret"),
            ("EntraId:ClientCredentials:0:SourceType", "unknown-source"),
            ("EntraId:ClientCredentials:0:ClientSecret", "synthetic-invalid-value")
        ];
        foreach (var (key, value) in cases)
        {
            var settings = OfflineHost.EnabledSettings();
            settings[$"Stage:Authentication:{key}"] = value;
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions { Args = [], ContentRootPath = AppContext.BaseDirectory });
            builder.Logging.ClearProviders();
            builder.Configuration.Sources.Clear();
            builder.Configuration.AddInMemoryCollection(settings);
            var error = Catch.Exception(() => EmittedAuthentication.Services(builder));
            _rejected.Add(error?.GetType().Name == "InvalidHostAuthentication");
            _safeDiagnostics.Add(error?.Message.Contains("Stage:Authentication", StringComparison.Ordinal) == true &&
                !error.Message.Contains("synthetic-invalid-value", StringComparison.Ordinal));
        }
    }

    [Fact] void should_fail_before_startup_instead_of_falling_back_to_anonymous() => _rejected.ShouldEqual(Enumerable.Repeat(true, 15));
    [Fact] void should_name_the_configuration_section_without_echoing_values() => _safeDiagnostics.ShouldEqual(Enumerable.Repeat(true, 15));
}
