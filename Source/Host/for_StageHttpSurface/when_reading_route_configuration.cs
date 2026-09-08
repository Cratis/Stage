// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_StageHttpSurface;

public class when_reading_route_configuration : Specification
{
    StageHttpRouteOptions _configured = null!;
    StageHttpRouteOptions _defaults = null!;

    void Because()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Cratis:Arc:GeneratedApis:EnableQueryHttpMethod"] = "false",
            ["Cratis:Arc:GeneratedApis:RoutePrefix"] = "other",
            ["Cratis:Arc:GeneratedApis:IncludeCommandNameInRoute"] = "false"
        }).Build();
        _configured = StageHttpRouteOptions.FromConfiguration(configuration);
        _defaults = StageHttpRouteOptions.FromConfiguration(new ConfigurationBuilder().Build());
    }

    [Fact] void should_keep_the_configured_query_opt_out_for_both_route_sets() => (_configured.Canonical.EnableQueryHttpMethod || _configured.Legacy.EnableQueryHttpMethod).ShouldBeFalse();
    [Fact] void should_keep_query_enabled_by_default() => (_defaults.Canonical.EnableQueryHttpMethod && _defaults.Legacy.EnableQueryHttpMethod).ShouldBeTrue();
    [Fact] void should_preserve_the_host_owned_api_prefix() => (_configured.Canonical.RoutePrefix == "api" && _configured.Legacy.RoutePrefix == "api").ShouldBeTrue();
    [Fact] void should_keep_names_canonical_and_legacy_grouping_separate() => (_configured.Canonical.IncludeCommandNameInRoute && !_configured.Legacy.IncludeCommandNameInRoute).ShouldBeTrue();
}
