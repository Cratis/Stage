// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Extensions;
using Cratis.Specifications;
using Cratis.Stage.Host.Extensions.Packages.given;
using Xunit;

namespace Cratis.Stage.Host.Extensions.Packages.for_ExtensionPackageLoader;

public class when_loading_from_the_cache : a_package_loader
{
    async Task Because()
    {
        Build();
        await Load();
        _result = await Load();
    }

    [Fact] void should_load_the_package() => _result.Loaded.Count.ShouldEqual(1);
    [Fact] void should_not_download_it_again() => _channel.Downloads.ShouldEqual(1);
    [Fact] void should_verify_the_cached_bytes_against_the_lock_each_time() => _channel.Opens.ShouldEqual(2);
}
