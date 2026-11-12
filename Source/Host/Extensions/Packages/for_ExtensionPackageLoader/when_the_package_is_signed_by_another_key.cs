// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Extensions;
using Cratis.Specifications;
using Cratis.Stage.Host.Extensions.Packages.given;
using Xunit;

namespace Cratis.Stage.Host.Extensions.Packages.for_ExtensionPackageLoader;

public class when_the_package_is_signed_by_another_key : a_package_loader
{
    async Task Because()
    {
        Build();
        using var stranger = PackageFixtures.NewKey();
        _channel = new(_archive, "extension", [PackageFixtures.Sign(stranger, PackageFixtures.CosignPayload(_entry.Digest))]);
        _result = await Load();
    }

    [Fact] void should_not_load_it() => _result.Loaded.ShouldBeEmpty();
    [Fact] void should_refuse_a_signature_that_is_not_the_recorded_publishers() => Refusal.Reason.ShouldEqual(PackageRefusalReason.SignatureInvalid);
}
