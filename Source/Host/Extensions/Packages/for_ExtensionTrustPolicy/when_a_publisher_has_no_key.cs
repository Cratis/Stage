// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Host.Extensions.Packages.given;
using Xunit;

namespace Cratis.Stage.Host.Extensions.Packages.for_ExtensionTrustPolicy;

public class when_a_publisher_has_no_key : Specification
{
    PackageTrust _trust = null!;
    PackageTrust _withoutSignatures = null!;

    void Because()
    {
        var options = new ExtensionPackageOptions();
        options.Allow.Publishers = [PackageFixtures.Publisher];
        var entry = PackageFixtures.Entry([1], [2]);
        _trust = new ExtensionTrustPolicy(options, ProjectExtensionPolicy.None).Evaluate(entry);
        options.RequireSignatures = false;
        _withoutSignatures = new ExtensionTrustPolicy(options, ProjectExtensionPolicy.None).Evaluate(entry);
    }

    [Fact] void should_refuse_because_no_signature_can_be_verified() => _trust.Allowed.ShouldBeFalse();
    [Fact] void should_say_no_key_is_configured() => _trust.Refusal.ShouldContain("No signing key");
    [Fact] void should_allow_it_only_when_the_deployment_turns_signatures_off() => _withoutSignatures.Allowed.ShouldBeTrue();
}
