// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisBackendApplicationScaffold.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisBackendApplicationScaffold;

public class when_creating_the_authentication_perimeter : a_current_scaffold
{
    [Fact] void should_register_after_the_unmanaged_configuration_hook() => Ordered("ConfigureServices(builder);", "MyApp.GeneratedHostAuthentication.ConfigureServices(builder);", "builder.Build()").ShouldBeTrue();
    [Fact] void should_establish_authentication_before_static_and_arc_handlers() => Ordered("MyApp.GeneratedHostAuthentication.ConfigureApplication(app);", "app.UseDefaultFiles();", "app.UseCratis();").ShouldBeTrue();
    [Fact] void should_not_emit_authentication_configuration() => Content("appsettings.json").ShouldNotContain("Authentication");
    [Fact] void should_emit_the_managed_helper() => Content("GeneratedHostAuthentication.cs").ShouldContain("internal static partial class GeneratedHostAuthentication");
    [Fact] void should_not_manage_user_configuration() => _first.Any(input => PathOf(input).StartsWith("Customizations/", StringComparison.Ordinal)).ShouldBeFalse();

    bool Ordered(string first, string second, string third)
    {
        var source = Content("Program.cs");
        var before = source.IndexOf(first, StringComparison.Ordinal);
        var middle = source.IndexOf(second, StringComparison.Ordinal);
        var after = source.IndexOf(third, StringComparison.Ordinal);
        return before >= 0 && middle > before && after > middle;
    }
}
