// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Extensions;
using Cratis.Specifications;
using Cratis.Stage.Host.Extensions.Packages.given;
using Xunit;

namespace Cratis.Stage.Host.Extensions.Packages.for_ExtensionPackageLoader;

public class when_the_contract_differs_from_the_lock : a_package_loader
{
    async Task Because()
    {
        Build();
        _entry = _entry with { ContractHash = ExtensionHashes.Of([9]) };
        _result = await Load();
    }

    [Fact] void should_not_load_it() => _result.Loaded.ShouldBeEmpty();
    [Fact] void should_refuse_it_for_its_contract() => Refusal.Reason.ShouldEqual(PackageRefusalReason.ContractMismatch);
}
