// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Extensions;
using Cratis.Stage.Host.Extensions.Packages.given;
using Xunit;

namespace Cratis.Stage.Host.Extensions.Packages.for_ExtensionPackageLoader;

public class when_a_signed_package_has_an_invalid_signature_and_signatures_are_not_required : a_package_loader
{
    async Task Because()
    {
        _options.RequireSignatures = false;
        Build();
        using var stranger = PackageFixtures.NewKey();
        _channel = new(_archive, "extension", [PackageFixtures.Sign(stranger, PackageFixtures.CosignPayload(_entry.Digest))]);
        _result = await Load();
    }

    [Fact] void should_still_refuse_a_signature_that_does_not_verify() => Refusal.Reason.ShouldEqual(PackageRefusalReason.SignatureInvalid);
}
