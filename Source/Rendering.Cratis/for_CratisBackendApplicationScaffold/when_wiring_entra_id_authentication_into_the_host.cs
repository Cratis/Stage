// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisBackendApplicationScaffold.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisBackendApplicationScaffold;

public class when_wiring_entra_id_authentication_into_the_host : a_current_scaffold
{
    [Fact] void should_register_sign_in_after_the_policies_and_before_the_application_services() => Ordered("Registration.Register(", "Authentication.Add(builder);", "ConfigureServices(builder);").ShouldBeTrue();
    [Fact] void should_build_the_application_after_registering_sign_in() => Ordered("Authentication.Add(builder);", "ConfigureServices(builder);", "builder.Build()").ShouldBeTrue();
    [Fact] void should_add_the_perimeter_right_after_building_and_before_serving_files() => Ordered("builder.Build()", "Authentication.Use(app);", "app.UseDefaultFiles();").ShouldBeTrue();
    [Fact] void should_keep_identity_web_out_of_the_perimeter() => Content("GeneratedAuthenticationPerimeter.cs").ShouldNotContain("Microsoft.Identity.Web");
    [Fact] void should_not_use_the_easy_auth_header_integration() => Content("GeneratedAuthentication.cs").ShouldNotContain("MicrosoftIdentityPlatform");
    [Fact] void should_not_resolve_the_settings_from_the_service_provider() => Content("GeneratedAuthentication.cs").ShouldNotContain("GetService<AuthenticationSettings>");
    [Fact] void should_keep_sign_in_out_of_the_application_settings() => Content("appsettings.json").ShouldNotContain("Authentication");
    [Fact] void should_map_every_health_check_with_exactly_one_route() => HealthCheckArguments().All(argument => argument.StartsWith('"') && argument.EndsWith('"') && !argument.Contains(',')).ShouldBeTrue();
    [Fact] void should_exempt_exactly_the_routes_the_host_maps_for_health() => PerimeterHealthRoutes().ShouldEqual(MappedHealthRoutes());
    [Fact] void should_name_the_entra_id_configuration_key() => Content("GeneratedAuthenticationPerimeter.cs").ShouldContain("Cratis:Authentication:EntraId");
    [Fact] void should_name_the_public_origin_configuration_key() => Content("GeneratedAuthenticationPerimeter.cs").ShouldContain("Cratis:Authentication:PublicOrigin");
    [Fact] void should_document_the_sign_in_configuration_in_the_readme() => Content("README.md").ShouldContain("## Sign in with Microsoft Entra ID");

    string PerimeterHealthRoutes() => string.Join('|', Between(Content("GeneratedAuthenticationPerimeter.cs"), "_healthRoutes = [", "]").Single().Split(',').Select(route => route.Trim().Trim('"')).Order(StringComparer.Ordinal));

    string MappedHealthRoutes() => string.Join('|', HealthCheckArguments().Select(argument => argument.Trim('"')).Order(StringComparer.Ordinal));

    IEnumerable<string> HealthCheckArguments() => Between(Content("Program.cs"), "MapHealthChecks(", ")");

    static IEnumerable<string> Between(string text, string start, string end)
    {
        var index = text.IndexOf(start, StringComparison.Ordinal);
        while (index >= 0)
        {
            var from = index + start.Length;
            yield return text[from..text.IndexOf(end, from, StringComparison.Ordinal)];
            index = text.IndexOf(start, from, StringComparison.Ordinal);
        }
    }

    bool Ordered(string first, string second, string third)
    {
        var source = Content("Program.cs");
        var before = source.IndexOf(first, StringComparison.Ordinal);
        var middle = source.IndexOf(second, StringComparison.Ordinal);
        var after = source.IndexOf(third, StringComparison.Ordinal);
        return before >= 0 && middle > before && after > middle;
    }
}
