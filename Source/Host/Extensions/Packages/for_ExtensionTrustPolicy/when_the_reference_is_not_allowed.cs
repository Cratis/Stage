// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Host.Extensions.Packages.given;
using Xunit;

namespace Cratis.Stage.Host.Extensions.Packages.for_ExtensionTrustPolicy;

public class when_the_reference_is_not_allowed : Specification
{
    PackageTrust _trust = null!;

    void Because()
    {
        var options = new ExtensionPackageOptions();
        options.Allow.Publishers = [PackageFixtures.Publisher];
        options.Allow.References = ["ghcr.io/acme/extension-ledger"];
        options.Publishers[PackageFixtures.Publisher] = new() { Keys = ["key"] };
        _trust = new ExtensionTrustPolicy(options, ProjectExtensionPolicy.None).Evaluate(PackageFixtures.Entry([1], [2]));
    }

    [Fact] void should_not_allow_a_package_from_an_allowed_publisher_that_was_not_asked_for() => _trust.Allowed.ShouldBeFalse();
    [Fact] void should_name_the_package() => _trust.Refusal.ShouldContain(PackageFixtures.Resolved);
}
