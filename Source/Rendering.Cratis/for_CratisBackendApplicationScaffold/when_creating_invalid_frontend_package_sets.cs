// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.Scaffolding;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisBackendApplicationScaffold;

public class when_creating_invalid_frontend_package_sets : Specification
{
    Exception _wildcardVersion = null!;
    Exception _caretRange = null!;
    Exception _tildeRange = null!;
    Exception _comparatorRange = null!;
    Exception _prereleaseVersion = null!;
    Exception _latestVersion = null!;
    Exception _partialVersion = null!;
    Exception _paddedVersion = null!;
    Exception _emptyVersion = null!;

    void Because()
    {
        _wildcardVersion = Catch.Exception(() => Profile(Set(arcPackageVersion: "*")));
        _caretRange = Catch.Exception(() => Profile(Set(arcReactPackageVersion: "^22.3.0")));
        _tildeRange = Catch.Exception(() => Profile(Set(arcVitePackageVersion: "~22.3.0")));
        _comparatorRange = Catch.Exception(() => Profile(Set(rxjsPackageVersion: ">=7.8.2 <8.0.0")));
        _prereleaseVersion = Catch.Exception(() => Profile(Set(fundamentalsPackageVersion: "7.18.1-beta.1")));
        _latestVersion = Catch.Exception(() => Profile(Set(tsyringePackageVersion: "latest")));
        _partialVersion = Catch.Exception(() => Profile(Set(reflectMetadataPackageVersion: "0.2")));
        _paddedVersion = Catch.Exception(() => Profile(Set(arcPackageVersion: " 22.3.0 ")));
        _emptyVersion = Catch.Exception(() => Profile(Set(rxjsPackageVersion: string.Empty)));
    }

    [Fact] void should_reject_a_wildcard_frontend_version() => _wildcardVersion.ShouldBeOfExactType<InvalidCratisBackendApplicationScaffold>();
    [Fact] void should_reject_a_caret_frontend_range() => _caretRange.ShouldBeOfExactType<InvalidCratisBackendApplicationScaffold>();
    [Fact] void should_reject_a_tilde_frontend_range() => _tildeRange.ShouldBeOfExactType<InvalidCratisBackendApplicationScaffold>();
    [Fact] void should_reject_a_comparator_frontend_range() => _comparatorRange.ShouldBeOfExactType<InvalidCratisBackendApplicationScaffold>();
    [Fact] void should_reject_a_prerelease_frontend_version() => _prereleaseVersion.ShouldBeOfExactType<InvalidCratisBackendApplicationScaffold>();
    [Fact] void should_reject_a_latest_frontend_version() => _latestVersion.ShouldBeOfExactType<InvalidCratisBackendApplicationScaffold>();
    [Fact] void should_reject_a_partial_frontend_version() => _partialVersion.ShouldBeOfExactType<InvalidCratisBackendApplicationScaffold>();
    [Fact] void should_reject_a_padded_frontend_version() => _paddedVersion.ShouldBeOfExactType<InvalidCratisBackendApplicationScaffold>();
    [Fact] void should_reject_an_empty_frontend_version() => _emptyVersion.ShouldBeOfExactType<InvalidCratisBackendApplicationScaffold>();

    static CratisFrontendPackageSet Set(
        string arcPackageVersion = "22.3.0",
        string arcReactPackageVersion = "22.3.0",
        string arcVitePackageVersion = "22.3.0",
        string fundamentalsPackageVersion = "7.18.1",
        string rxjsPackageVersion = "7.8.2",
        string tsyringePackageVersion = "4.10.0",
        string reflectMetadataPackageVersion = "0.2.2") =>
        CratisFrontendPackageSet.Create(
            arcPackageVersion,
            arcReactPackageVersion,
            arcVitePackageVersion,
            fundamentalsPackageVersion,
            rxjsPackageVersion,
            tsyringePackageVersion,
            reflectMetadataPackageVersion);

    static CratisBackendApplicationScaffoldProfile Profile(CratisFrontendPackageSet frontendPackageSet) =>
        CratisBackendApplicationScaffoldProfile.Create(
            "1",
            "net10.0",
            "22.3.0",
            "22.3.0",
            "22.3.0",
            "4.0.0",
            "4.0.0",
            "18.9.0",
            "6.2.0",
            "2.9.3",
            "4.0.0",
            "16.35.3",
            frontendPackageSet);
}
