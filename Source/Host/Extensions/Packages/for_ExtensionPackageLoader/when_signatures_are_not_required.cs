// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Extensions;
using Cratis.Stage.Host.Extensions.Packages.given;
using Xunit;

namespace Cratis.Stage.Host.Extensions.Packages.for_ExtensionPackageLoader;

public class when_signatures_are_not_required : a_package_loader
{
    async Task Because()
    {
        _options.RequireSignatures = false;
        Build(sign: false);
        _result = await Load();
    }

    [Fact] void should_load_the_package() => _result.Loaded.Count.ShouldEqual(1);
    [Fact] void should_not_say_it_was_signed() => _result.Loaded[0].Signed.ShouldBeFalse();
}
