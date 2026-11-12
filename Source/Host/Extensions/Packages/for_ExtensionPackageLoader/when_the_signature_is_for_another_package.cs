// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Extensions;
using Cratis.Stage.Host.Extensions.Packages.given;
using Xunit;

namespace Cratis.Stage.Host.Extensions.Packages.for_ExtensionPackageLoader;

public class when_the_signature_is_for_another_package : a_package_loader
{
    async Task Because()
    {
        Build();
        var other = "sha256:" + new string('c', 64);
        _channel = new(_archive, "extension", [PackageFixtures.Sign(_key, PackageFixtures.CosignPayload(other))]);
        _result = await Load();
    }

    [Fact] void should_refuse_a_genuine_signature_that_covers_something_else() => Refusal.Reason.ShouldEqual(PackageRefusalReason.SignatureInvalid);
}
