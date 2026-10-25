// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.Scaffolding;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Naming.Imports.for_FrameworkNameCatalog;

public class when_reading_the_checked_in_catalog : Specification
{
    FrameworkNameCatalog _catalog = null!;

    void Because() => _catalog = FrameworkNameCatalog.Current;

    [Fact] void should_be_generated_for_the_current_scaffold_version() => _catalog.Content.ScaffoldVersion.ShouldEqual(CratisBackendApplicationScaffoldProfile.Current.Version);
    [Fact] void should_record_the_current_scaffold_pins() => Pins(_catalog.Content.Pins).ShouldEqual(Pins(ScaffoldPins.Of(CratisBackendApplicationScaffoldProfile.Current)));
    [Fact] void should_restore_every_pinned_package_at_its_pinned_version() =>
        _catalog.Content.Pins.Where(pin => pin.Key is not ("TargetFramework" or "TargetingPackVersion") &&
            !_catalog.Content.Packages.Any(package => package.Id == pin.Key && package.Version == pin.Value)).ShouldBeEmpty();
    [Fact] void should_import_the_cratis_package_global_usings_in_both_projects() =>
        _catalog.GlobalUsings(GeneratedProjects.Specifications).ShouldContain("Cratis.Chronicle.Events");
    [Fact] void should_import_the_web_sdk_usings_in_specifications_compiled_into_the_application() =>
        (_catalog.GlobalUsings(GeneratedProjects.Application).Contains("Microsoft.AspNetCore.Http") &&
            _catalog.GlobalUsings(GeneratedProjects.Specifications).Contains("Microsoft.AspNetCore.Http")).ShouldBeTrue();
    [Fact] void should_import_the_test_package_usings_in_the_application_debug_build() =>
        _catalog.GlobalUsings(GeneratedProjects.Application).ShouldContain("Orleans");
    [Fact] void should_know_both_identity_providers() =>
        (_catalog.TypesNamed("Cratis.Arc.Identity", "IIdentityProvider", 0, GeneratedProjects.Application).Any() &&
            _catalog.TypesNamed("Cratis.Chronicle.Identities", "IIdentityProvider", 0, GeneratedProjects.Application).Any()).ShouldBeTrue();
    [Fact] void should_know_what_a_concept_inherits() => _catalog.Inherited("Cratis.Concepts.ConceptAs`1", GeneratedProjects.Application)!.Members.ShouldContain("Value");

    static string Pins(IEnumerable<KeyValuePair<string, string>> pins) => string.Join(' ', pins.Select(pin => $"{pin.Key}={pin.Value}"));
}
