// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Host.Extensions.Packages.given;
using Xunit;

namespace Cratis.Stage.Host.Extensions.Packages.for_ExtensionTrustPolicy;

public class when_the_publisher_is_not_allowed : Specification
{
    PackageTrust _trust = null!;

    void Because()
    {
        var options = new ExtensionPackageOptions();
        options.Allow.Publishers = ["https://github.com/cratis"];
        _trust = new ExtensionTrustPolicy(options, ProjectExtensionPolicy.None).Evaluate(PackageFixtures.Entry([1], [2]));
    }

    [Fact] void should_not_allow_the_package() => _trust.Allowed.ShouldBeFalse();
    [Fact] void should_name_the_publisher() => _trust.Refusal.ShouldContain(PackageFixtures.Publisher);
}
