// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Extensions;
using Cratis.Specifications;
using Cratis.Stage.Host.Extensions.Packages.given;
using Xunit;

namespace Cratis.Stage.Host.Extensions.Packages.for_ExtensionPackageLoader;

public class when_a_placeholder_runs_a_script : a_package_loader
{
    async Task Because()
    {
        _files = PackageFixtures.Files(placeholder: "<html><script>alert(1)</script></html>");
        Build();
        _result = await Load();
    }

    [Fact] void should_not_load_it() => _result.Loaded.ShouldBeEmpty();
    [Fact] void should_refuse_it_as_malformed() => Refusal.Reason.ShouldEqual(PackageRefusalReason.Malformed);
    [Fact] void should_say_which_placeholder() => Refusal.Details.ShouldContain("placeholders/preview.html");
}
