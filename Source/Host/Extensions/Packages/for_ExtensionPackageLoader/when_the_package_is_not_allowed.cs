// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Extensions;
using Cratis.Specifications;
using Cratis.Stage.Host.Extensions.Packages.given;
using Xunit;

namespace Cratis.Stage.Host.Extensions.Packages.for_ExtensionPackageLoader;

public class when_the_package_is_not_allowed : a_package_loader
{
    async Task Because()
    {
        _options.Allow.Publishers = ["https://github.com/someone-else"];
        Build();
        _result = await Load();
    }

    [Fact] void should_not_load_it() => _result.Loaded.ShouldBeEmpty();
    [Fact] void should_refuse_it_as_not_allowed() => Refusal.Reason.ShouldEqual(PackageRefusalReason.NotAllowed);
    [Fact] void should_never_fetch_what_is_not_allowed() => _channel.Downloads.ShouldEqual(0);
    [Fact] void should_report_it_as_an_admission_issue() => Refusal.ToIssue().Capability.ShouldEqual(PackageRefusal.Capability);
    [Fact] void should_name_the_package_in_the_issue() => Refusal.ToIssue().Details.ShouldContain(PackageFixtures.Reference);
}
