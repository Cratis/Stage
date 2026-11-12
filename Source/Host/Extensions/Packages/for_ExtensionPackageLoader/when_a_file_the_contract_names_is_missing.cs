// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Extensions;
using Cratis.Specifications;
using Cratis.Stage.Host.Extensions.Packages.given;
using Xunit;

namespace Cratis.Stage.Host.Extensions.Packages.for_ExtensionPackageLoader;

public class when_a_file_the_contract_names_is_missing : a_package_loader
{
    async Task Because()
    {
        _files.Remove("frontend/index.js");
        Build();
        _result = await Load();
    }

    [Fact] void should_refuse_it_as_malformed() => Refusal.Reason.ShouldEqual(PackageRefusalReason.Malformed);
    [Fact] void should_say_what_is_missing() => Refusal.Details.ShouldContain("frontend/index.js");
}
