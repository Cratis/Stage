// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Extensions;
using Cratis.Stage.Host.Extensions.Packages.given;
using Xunit;

namespace Cratis.Stage.Host.Extensions.Packages.for_ExtensionPackageLoader;

public class when_the_lock_entry_is_malformed : a_package_loader
{
    async Task Because()
    {
        Build();
        _entry = _entry with { Digest = "latest" };
        _result = await Load();
    }

    [Fact] void should_refuse_it_as_malformed() => Refusal.Reason.ShouldEqual(PackageRefusalReason.Malformed);
    [Fact] void should_not_fetch_anything() => _channel.Downloads.ShouldEqual(0);
}
