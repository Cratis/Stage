// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Extensions;
using Cratis.Stage.Host.Extensions.Packages.given;
using Xunit;

namespace Cratis.Stage.Host.Extensions.Packages.for_ExtensionPackageLoader;

public class when_loading_a_signed_and_allowed_package : a_package_loader
{
    async Task Because()
    {
        Build();
        _result = await Load();
    }

    [Fact] void should_load_the_package() => _result.Loaded.Count.ShouldEqual(1);
    [Fact] void should_not_refuse_anything() => _result.Refusals.ShouldBeEmpty();
    [Fact] void should_read_the_manifest() => _result.Loaded[0].Contract.DisplayName.ShouldEqual("Transactional mail");
    [Fact] void should_record_that_the_signature_was_verified() => _result.Loaded[0].Signed.ShouldBeTrue();
    [Fact] void should_pin_the_locked_digest() => _result.Loaded[0].Digest.ShouldEqual(_entry.Digest);
    [Fact] void should_download_it_once() => _channel.Downloads.ShouldEqual(1);
    [Fact] void should_keep_the_contract_for_the_compiler() => new ExtensionCache(Path.Combine(_folder, "contracts")).TryRead(_entry.Digest).ShouldNotBeNull();
    [Fact] void should_find_it_by_the_reference_the_model_names() => _result.FindByReference(PackageFixtures.Reference).ShouldNotBeNull();
    [Fact] void should_find_it_by_digest() => _result.FindByDigest(_entry.Digest).ShouldNotBeNull();
}
