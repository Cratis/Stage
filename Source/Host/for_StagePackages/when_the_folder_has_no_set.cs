// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Host.for_StagePackages;

public class when_the_folder_has_no_set : Specification
{
    string _root = null!;
    StagePackages? _packages;

    void Establish() => _root = Directory.CreateTempSubdirectory("stage-packages-empty-spec").FullName;
    void Because() => _packages = StagePackages.Read(_root);
    void Destroy() => Directory.Delete(_root, recursive: true);

    [Fact] void should_have_no_packages() => _packages.ShouldBeNull();
    [Fact] void should_have_none_when_no_folder_is_configured() => StagePackages.Read(null).ShouldBeNull();
    [Fact] void should_have_none_when_the_folder_is_missing() => StagePackages.Read(Path.Combine(_root, "missing")).ShouldBeNull();
}
