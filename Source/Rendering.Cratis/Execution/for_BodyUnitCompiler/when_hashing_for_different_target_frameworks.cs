// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Execution;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Execution.for_BodyUnitCompiler;

[PerFactSpecificationLifecycle]
public class when_hashing_for_different_target_frameworks : Specification
{
    string _directory = null!;
    BodyUnitEntry _version3 = null!;
    BodyUnitEntry _version3Again = null!;
    BodyUnitEntry _version4 = null!;

    void Establish() => _directory = Directory.CreateTempSubdirectory("stage-framework-hash-spec-").FullName;
    void Destroy() => Directory.Delete(_directory, true);

    void Because()
    {
        var loaded = validation_body_model.Load(validation_body_model.Command("return context.Value > 0;"));
        _version3 = BodyUnitCompiler.Compile(loaded, Path.Combine(_directory, "a")).Entries.Single();
        _version3Again = BodyUnitCompiler.Compile(loaded, Path.Combine(_directory, "b"), profileVersion: null).Entries.Single();
        _version4 = BodyUnitCompiler.Compile(loaded, Path.Combine(_directory, "c"), "4").Entries.Single();
    }

    [Fact] void should_admit_the_version_4_unit() => _version4.Admitted.ShouldBeTrue();
    [Fact] void should_keep_the_version_3_hash_stable() => _version3Again.UnitHash.ShouldEqual(_version3.UnitHash);
    [Fact] void should_not_share_a_cache_key_across_frameworks() => _version4.UnitHash.ShouldNotEqual(_version3.UnitHash);
    [Fact] void should_use_the_net11_framework_for_a_version_4_context() => ContextFramework("4").ShouldEqual("net11.0");
    [Fact] void should_use_the_net10_framework_without_a_version() => ContextFramework(null).ShouldEqual("net10.0");

    static string ContextFramework(string? version) =>
        BodyUnitCompiler.CreateContext(validation_body_model.Load(validation_body_model.Command("return true;")), version).TargetFramework;
}
