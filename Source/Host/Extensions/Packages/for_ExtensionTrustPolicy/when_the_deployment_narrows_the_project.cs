// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Host.Extensions.Packages.given;
using Xunit;

namespace Cratis.Stage.Host.Extensions.Packages.for_ExtensionTrustPolicy;

public class when_the_deployment_narrows_the_project : Specification
{
    PackageTrust _trust = null!;

    void Because()
    {
        var project = new ProjectExtensionPolicy(
            new() { Publishers = [PackageFixtures.Publisher] },
            new Dictionary<string, PublisherOptions> { [PackageFixtures.Publisher] = new() { Keys = ["key"] } },
            "project",
            string.Empty);
        var deployment = new ExtensionPackageOptions();
        deployment.Allow.References = ["ghcr.io/cratis/*"];
        _trust = new ExtensionTrustPolicy(deployment, project).Evaluate(PackageFixtures.Entry([1], [2]));
    }

    [Fact] void should_refuse_what_the_deployment_does_not_allow_even_though_the_project_does() => _trust.Allowed.ShouldBeFalse();
    [Fact] void should_say_it_was_the_deployment() => _trust.Refusal.ShouldContain("this deployment");
}
