// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.Scaffolding;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisBackendApplicationScaffold;

public class when_creating_invalid_contracts : Specification
{
    Exception _applicationName = null!;
    Exception _projectName = null!;
    Exception _rootNamespace = null!;
    Exception _wildcardVersion = null!;
    Exception _rangeVersion = null!;
    Exception _latestImage = null!;
    Exception _invalidTargetFramework = null!;
    Exception _invalidProfileVersion = null!;
    Exception _missingRequest = null!;

    void Because()
    {
        _applicationName = Catch.Exception(() => CratisBackendApplicationScaffoldRequest.Create("../MyApp", "MyApp", "MyApp"));
        _projectName = Catch.Exception(() => CratisBackendApplicationScaffoldRequest.Create("MyApp", "MyApp/Backend", "MyApp"));
        _rootNamespace = Catch.Exception(() => CratisBackendApplicationScaffoldRequest.Create("MyApp", "MyApp", "MyApp-backend"));
        _wildcardVersion = Catch.Exception(() => Profile(cratisPackageVersion: "*"));
        _rangeVersion = Catch.Exception(() => Profile(cratisPackageVersion: "[22.0.0,23.0.0)"));
        _latestImage = Catch.Exception(() => Profile(chronicleImageVersion: "latest"));
        _invalidTargetFramework = Catch.Exception(() => Profile(targetFramework: "net10"));
        _invalidProfileVersion = Catch.Exception(() => Profile(version: "0"));
        _missingRequest = Catch.Exception(() => new CratisBackendApplicationScaffold().Create(null!));
    }

    [Fact] void should_reject_the_application_name_with_the_project_exception() => _applicationName.ShouldBeOfExactType<InvalidCratisBackendApplicationScaffold>();
    [Fact] void should_reject_the_project_name_with_the_project_exception() => _projectName.ShouldBeOfExactType<InvalidCratisBackendApplicationScaffold>();
    [Fact] void should_reject_the_root_namespace_with_the_project_exception() => _rootNamespace.ShouldBeOfExactType<InvalidCratisBackendApplicationScaffold>();
    [Fact] void should_reject_a_wildcard_package_version() => _wildcardVersion.ShouldBeOfExactType<InvalidCratisBackendApplicationScaffold>();
    [Fact] void should_reject_a_package_version_range() => _rangeVersion.ShouldBeOfExactType<InvalidCratisBackendApplicationScaffold>();
    [Fact] void should_reject_a_latest_image_tag() => _latestImage.ShouldBeOfExactType<InvalidCratisBackendApplicationScaffold>();
    [Fact] void should_reject_a_non_exact_target_framework() => _invalidTargetFramework.ShouldBeOfExactType<InvalidCratisBackendApplicationScaffold>();
    [Fact] void should_reject_a_non_positive_profile_version() => _invalidProfileVersion.ShouldBeOfExactType<InvalidCratisBackendApplicationScaffold>();
    [Fact] void should_reject_a_missing_request_with_the_project_exception() => _missingRequest.ShouldBeOfExactType<InvalidCratisBackendApplicationScaffold>();

    static CratisBackendApplicationScaffoldProfile Profile(
        string version = "1",
        string targetFramework = "net10.0",
        string cratisPackageVersion = "22.3.0",
        string chronicleImageVersion = "16.35.3") =>
        CratisBackendApplicationScaffoldProfile.Create(
            version,
            targetFramework,
            cratisPackageVersion,
            "22.3.0",
            "22.3.0",
            "4.0.0",
            "4.0.0",
            "18.9.0",
            "6.2.0",
            "2.9.3",
            "4.0.0",
            chronicleImageVersion);
}
