// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Host.Extensions.Activation.given;
using Cratis.Stage.Host.Extensions.Packages.given;
using Xunit;

namespace Cratis.Stage.Host.Extensions.Activation.for_ExecutionTierPlanner;

public class when_planning_the_tier : a_backend_package
{
    ExecutionOptions _options = null!;
    ExecutionPlan _frontendOnly = null!;
    ExecutionPlan _native = null!;
    ExecutionPlan _shared = null!;
    ExecutionPlan _sharedWithoutHost = null!;
    ExecutionPlan _untrusted = null!;
    ExecutionPlan _container = null!;

    async Task Because()
    {
        _options = new();
        _options.Sharing.Publishers = [PackageFixtures.Publisher];
        _options.Hosts["dotnet"] = new() { Command = "stage-dotnet-host" };
        _options.Containers["Isolated"] = new() { Image = "ghcr.io/acme/mail:1" };
        var planner = new ExecutionTierPlanner(_options);

        _frontendOnly = planner.Plan("Mail", await LoadFrontendOnly());
        _native = planner.Plan("Mail", await LoadBackend("native"));
        _shared = planner.Plan("Mail", await LoadBackend("dotnet"));
        _sharedWithoutHost = planner.Plan("Mail", await LoadBackend("node"));

        _options.Sharing.Publishers = [];
        _untrusted = planner.Plan("Mail", await LoadBackend("dotnet"));
        _container = planner.Plan("Isolated", await LoadBackend("dotnet"));
    }

    [Fact] void should_run_no_process_for_a_frontend_only_extension() => (_frontendOnly.Tier, _frontendOnly.CanRun).ShouldEqual((ExecutionTier.None, false));
    [Fact] void should_run_a_native_backend_as_a_process_of_its_own() => (_native.Tier, _native.CanRun).ShouldEqual((ExecutionTier.Native, true));
    [Fact] void should_share_a_host_with_a_trusted_publishers_extension() => (_shared.Tier, _shared.CanRun).ShouldEqual((ExecutionTier.SharedHost, true));
    [Fact] void should_refuse_to_share_a_host_nobody_configured() => (_sharedWithoutHost.Tier, _sharedWithoutHost.CanRun).ShouldEqual((ExecutionTier.SharedHost, false));
    [Fact] void should_name_the_missing_host() => _sharedWithoutHost.Refusal.ShouldContain("Hosts:node");
    [Fact] void should_give_an_untrusted_extension_a_container_it_does_not_have() => (_untrusted.Tier, _untrusted.CanRun).ShouldEqual((ExecutionTier.Container, false));
    [Fact] void should_name_the_missing_container() => _untrusted.Refusal.ShouldContain("Containers:Mail");
    [Fact] void should_run_an_untrusted_extension_in_its_container() => (_container.Tier, _container.CanRun).ShouldEqual((ExecutionTier.Container, true));
}
