// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisBackendApplicationScaffold.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisBackendApplicationScaffold;

public class when_creating_an_unmanaged_composition_seam : a_current_scaffold
{
    [Fact] void should_import_optional_unmanaged_dependencies() => Content("MyApp.csproj").ShouldContain("<Import Project=\"Customizations/Dependencies.props\" Condition=\"Exists('Customizations/Dependencies.props')\" />");
    [Fact] void should_register_services_after_cratis_and_before_build() => Ordered("builder.AddCratis(", "ConfigureServices(builder);", "builder.Build()").ShouldBeTrue();
    [Fact] void should_configure_application_after_cratis_and_before_run() => Ordered("app.UseCratis();", "ConfigureApplication(app);", "await app.RunAsync()").ShouldBeTrue();
    [Fact] void should_erase_unimplemented_hooks() => Content("Program.cs").ShouldContain("static partial void ConfigureServices(WebApplicationBuilder builder);");
    [Fact] void should_not_manage_user_code() => _first.Any(input => PathOf(input).StartsWith("Customizations/", StringComparison.Ordinal)).ShouldBeFalse();

    bool Ordered(string first, string second, string third)
    {
        var source = Content("Program.cs");
        var before = source.IndexOf(first, StringComparison.Ordinal);
        var middle = source.IndexOf(second, StringComparison.Ordinal);
        var after = source.IndexOf(third, StringComparison.Ordinal);
        return before >= 0 && middle > before && after > middle;
    }
}
