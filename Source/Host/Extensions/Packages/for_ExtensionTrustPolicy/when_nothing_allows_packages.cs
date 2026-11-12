// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Host.Extensions.Packages.given;
using Xunit;

namespace Cratis.Stage.Host.Extensions.Packages.for_ExtensionTrustPolicy;

public class when_nothing_allows_packages : Specification
{
    PackageTrust _trust = null!;

    void Because()
    {
        var entry = PackageFixtures.Entry([1], [2]);
        _trust = new ExtensionTrustPolicy(new(), ProjectExtensionPolicy.None).Evaluate(entry);
    }

    [Fact] void should_not_allow_the_package_because_a_model_names_it() => _trust.Allowed.ShouldBeFalse();
    [Fact] void should_say_what_to_configure() => _trust.Refusal.ShouldContain("Allow");
}
