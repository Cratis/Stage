// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisBackendApplicationScaffold.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_GeneratedHostAuthentication;

public class when_ordering_the_generated_host : a_current_scaffold
{
    bool _services;
    bool _middleware;

    void Because()
    {
        var program = Content("Program.cs");
        _services = Ordered(program, "AddCratis", "ConfigureServices(builder);", "GeneratedHostAuthentication.ConfigureServices(builder);", "builder.Build()");
        _middleware = Ordered(program, "builder.Build()", "GeneratedHostAuthentication.ConfigureApplication(app);", "app.UseDefaultFiles()", "app.UseStaticFiles()", "app.UseCratis()");
    }

    [Fact] void should_register_generated_selectors_after_cratis_and_custom_services() => _services.ShouldBeTrue();
    [Fact] void should_install_the_perimeter_before_static_files_and_cratis_endpoints() => _middleware.ShouldBeTrue();

    static bool Ordered(string source, params string[] calls)
    {
        var previous = -1;
        foreach (var call in calls)
        {
            var position = source.IndexOf(call, previous + 1, StringComparison.Ordinal);
            if (position < 0)
            {
                return false;
            }

            previous = position;
        }

        return true;
    }
}
