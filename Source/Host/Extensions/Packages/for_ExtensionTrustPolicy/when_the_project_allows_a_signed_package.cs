// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Host.Extensions.Packages.given;
using Xunit;

namespace Cratis.Stage.Host.Extensions.Packages.for_ExtensionTrustPolicy;

public class when_the_project_allows_a_signed_package : Specification
{
    PackageTrust _trust = null!;

    void Because()
    {
        var project = new ProjectExtensionPolicy(
            new() { Publishers = ["https://github.com/acme*"], References = ["ghcr.io/acme/*"] },
            new Dictionary<string, PublisherOptions> { [PackageFixtures.Publisher] = new() { Keys = ["the-key"] } },
            "project",
            string.Empty);
        _trust = new ExtensionTrustPolicy(new(), project).Evaluate(PackageFixtures.Entry([1], [2]));
    }

    [Fact] void should_allow_it() => _trust.Allowed.ShouldBeTrue();
    [Fact] void should_use_the_projects_keys() => _trust.Keys.ShouldContainOnly("the-key");
    [Fact] void should_require_a_signature() => _trust.RequireSignature.ShouldBeTrue();
}
