// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Extensions;
using Cratis.Specifications;
using Cratis.Stage.Host.Extensions.Packages.given;
using Xunit;

namespace Cratis.Stage.Host.Extensions.Packages.for_ExtensionPackageLoader;

public class when_a_cached_copy_was_tampered_with : a_package_loader
{
    async Task Because()
    {
        Build();
        await Load();
        File.WriteAllBytes(Path.Combine(_options.CacheDirectory, _entry.Digest.Replace(':', '-'), "archive"), [1, 2, 3]);
        _result = await Load();
    }

    [Fact] void should_download_it_again_rather_than_trust_the_cache() => _channel.Downloads.ShouldEqual(2);
    [Fact] void should_load_the_package_that_matches_the_lock() => _result.Loaded.Count.ShouldEqual(1);
}
