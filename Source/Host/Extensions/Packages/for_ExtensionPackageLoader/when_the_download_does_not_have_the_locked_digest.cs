// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Extensions;
using Cratis.Stage.Host.Extensions.Packages.given;
using Xunit;

namespace Cratis.Stage.Host.Extensions.Packages.for_ExtensionPackageLoader;

public class when_the_download_does_not_have_the_locked_digest : a_package_loader
{
    async Task Because()
    {
        Build();
        _channel.Archive = PackageFixtures.Archive("extension", PackageFixtures.Files(PackageFixtures.Contract(extra: "  # changed")));
        _result = await Load();
    }

    [Fact] void should_not_load_it() => _result.Loaded.ShouldBeEmpty();
    [Fact] void should_refuse_it_for_its_digest() => Refusal.Reason.ShouldEqual(PackageRefusalReason.DigestMismatch);
    [Fact] void should_not_keep_what_did_not_verify() => Directory.Exists(Path.Combine(_options.CacheDirectory, _entry.Digest.Replace(':', '-'))).ShouldBeFalse();
}
