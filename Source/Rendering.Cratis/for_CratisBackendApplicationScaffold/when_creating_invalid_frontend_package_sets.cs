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
    Exception _reactRange = null!;
    Exception _reactDomWildcard = null!;
    Exception _routerPrerelease = null!;
    Exception _viteLatest = null!;
    Exception _typeScriptPartial = null!;
    Exception _vitePluginReactPadded = null!;
    Exception _typesReactRange = null!;
    Exception _typesReactDomEmpty = null!;

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
        _reactRange = Catch.Exception(() => Profile(Set(reactPackageVersion: "^19.0.8")));
        _reactDomWildcard = Catch.Exception(() => Profile(Set(reactDomPackageVersion: "*")));
        _routerPrerelease = Catch.Exception(() => Profile(Set(reactRouterDomPackageVersion: "6.30.6-rc.1")));
        _viteLatest = Catch.Exception(() => Profile(Set(vitePackageVersion: "latest")));
        _typeScriptPartial = Catch.Exception(() => Profile(Set(typeScriptPackageVersion: "7.0")));
        _vitePluginReactPadded = Catch.Exception(() => Profile(Set(vitePluginReactPackageVersion: " 6.1.0")));
        _typesReactRange = Catch.Exception(() => Profile(Set(typesReactPackageVersion: "~19.2.18")));
        _typesReactDomEmpty = Catch.Exception(() => Profile(Set(typesReactDomPackageVersion: string.Empty)));
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
    [Fact] void should_reject_a_react_range() => _reactRange.ShouldBeOfExactType<InvalidCratisBackendApplicationScaffold>();
    [Fact] void should_reject_a_react_dom_wildcard() => _reactDomWildcard.ShouldBeOfExactType<InvalidCratisBackendApplicationScaffold>();
    [Fact] void should_reject_a_prerelease_router_version() => _routerPrerelease.ShouldBeOfExactType<InvalidCratisBackendApplicationScaffold>();
    [Fact] void should_reject_a_latest_vite_version() => _viteLatest.ShouldBeOfExactType<InvalidCratisBackendApplicationScaffold>();
    [Fact] void should_reject_a_partial_typescript_version() => _typeScriptPartial.ShouldBeOfExactType<InvalidCratisBackendApplicationScaffold>();
    [Fact] void should_reject_a_padded_vite_plugin_react_version() => _vitePluginReactPadded.ShouldBeOfExactType<InvalidCratisBackendApplicationScaffold>();
    [Fact] void should_reject_a_react_types_range() => _typesReactRange.ShouldBeOfExactType<InvalidCratisBackendApplicationScaffold>();
    [Fact] void should_reject_an_empty_react_dom_types_version() => _typesReactDomEmpty.ShouldBeOfExactType<InvalidCratisBackendApplicationScaffold>();

    static CratisFrontendPackageSet Set(
        string arcPackageVersion = "22.16.1",
        string arcReactPackageVersion = "22.16.1",
        string arcVitePackageVersion = "22.16.1",
        string fundamentalsPackageVersion = "7.19.3",
        string rxjsPackageVersion = "7.8.2",
        string tsyringePackageVersion = "4.10.0",
        string reflectMetadataPackageVersion = "0.2.2",
        string reactPackageVersion = "19.3.0",
        string reactDomPackageVersion = "19.3.0",
        string reactRouterDomPackageVersion = "7.18.4",
        string vitePackageVersion = "8.3.0",
        string typeScriptPackageVersion = "7.0.2",
        string vitePluginReactPackageVersion = "6.1.1",
        string typesReactPackageVersion = "19.3.0",
        string typesReactDomPackageVersion = "19.3.0",
        string componentsPackageVersion = "4.9.0",
        string scenePackageVersion = "3.5.0",
        string primeReactPackageVersion = "11.1.0",
        string primeIconsPackageVersion = "8.0.1",
        string primeUixThemesPackageVersion = "3.0.1") =>
        CratisFrontendPackageSet.Create(
            arcPackageVersion,
            arcReactPackageVersion,
            arcVitePackageVersion,
            fundamentalsPackageVersion,
            rxjsPackageVersion,
            tsyringePackageVersion,
            reflectMetadataPackageVersion,
            reactPackageVersion,
            reactDomPackageVersion,
            reactRouterDomPackageVersion,
            vitePackageVersion,
            typeScriptPackageVersion,
            vitePluginReactPackageVersion,
            typesReactPackageVersion,
            typesReactDomPackageVersion,
            componentsPackageVersion,
            scenePackageVersion,
            primeReactPackageVersion,
            primeIconsPackageVersion,
            primeUixThemesPackageVersion);

    static CratisBackendApplicationScaffoldProfile Profile(CratisFrontendPackageSet frontendPackageSet) =>
        CratisBackendApplicationScaffoldProfile.Create(
            "1",
            "net10.0",
            "22.16.1",
            "22.16.1",
            "22.16.1",
            "4.0.0",
            "4.0.0",
            "18.9.0",
            "6.2.0",
            "2.9.3",
            "4.0.0",
            "16.35.3",
            frontendPackageSet);
}
