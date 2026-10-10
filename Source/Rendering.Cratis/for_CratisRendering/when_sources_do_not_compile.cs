// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRendering;

public class when_sources_do_not_compile : given.a_source_plan
{
    void Establish() => File.WriteAllText(Path.Combine(_root, "broken.play"), "not valid screenplay !!!");
    async Task Because() => _result = await From("broken.play");
    [Fact] void should_refuse_compilation() => ShouldRefuse("STAGE-PLAN-003");
    [Fact] void should_preserve_play_codes_and_locations() => _result.Diagnostics.Any(diagnostic => diagnostic.Code.StartsWith("PLAY", StringComparison.Ordinal) && diagnostic.Source!.StartsWith("broken.play(", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_not_publish_partial_artifacts() => _result.Artifacts.ShouldBeEmpty();
}
