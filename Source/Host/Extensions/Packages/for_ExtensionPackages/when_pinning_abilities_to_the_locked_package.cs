// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Specifications;
using Cratis.Stage.Host.Extensions.Packages.given;
using Cratis.Stage.Host.Invocations;
using Xunit;

namespace Cratis.Stage.Host.Extensions.Packages.for_ExtensionPackages;

public class when_pinning_abilities_to_the_locked_package : a_package_loader
{
    PinnedAbilityCatalog _catalog = null!;
    Dictionary<string, string> _references = null!;

    static AbilityDescriptor Describe(string system, string digest) => new(new(system, "Send", 1), EffectClass.Once, ["sent"], digest);

    async Task Because()
    {
        Build();
        _result = await Load();
        _references = [];
        SystemExtensionReferences.Read(["# a comment", "system Mail", $"  extension \"{PackageFixtures.Reference}\" version 1", "  setting apiKey from secret Key", "", "system Billing", "  ability Charge", "system Ledger", "  extension \"acme/ledger\""], _references);
        _catalog = new(new AbilityCatalog([Describe("Mail", "sha256:old"), Describe("Billing", "sha256:kept")]), new ExtensionPackages(_result, _references));
    }

    [Fact] void should_read_which_extension_a_system_names() => _references["Mail"].ShouldEqual(PackageFixtures.Reference);
    [Fact] void should_read_a_system_that_names_a_package_that_is_not_loaded() => _references["Ledger"].ShouldEqual("acme/ledger");
    [Fact] void should_not_read_a_system_with_no_extension() => _references.ContainsKey("Billing").ShouldBeFalse();
    [Fact] void should_pin_every_ability_of_the_extension_to_the_locked_digest() => _catalog.Find("Mail", "Send")!.PackageDigest.ShouldEqual(_entry.Digest);
    [Fact] void should_pin_the_locked_digest_in_the_whole_catalog() => _catalog.All.Single(descriptor => descriptor.Reference.System == "Mail").PackageDigest.ShouldEqual(_entry.Digest);
    [Fact] void should_leave_other_systems_as_they_are() => _catalog.Find("Billing", "Send")!.PackageDigest.ShouldEqual("sha256:kept");
}
