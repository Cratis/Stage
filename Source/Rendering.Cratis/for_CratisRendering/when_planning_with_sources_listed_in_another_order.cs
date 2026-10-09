// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRendering;

public class when_planning_with_sources_listed_in_another_order : given.a_source_plan
{
    CratisPlanResult _original = null!;
    async Task Establish() => _original = await From("projects.play", "tasks.play", "context.play");
    async Task Because() => _result = await From("context.play", "tasks.play", "projects.play");
    [Fact] void should_plan_successfully() => ShouldSucceed();
    [Fact] void should_preserve_the_digest() => _result.Digest.ShouldEqual(_original.Digest);
    [Fact] void should_preserve_every_byte() => _result.Artifacts.Zip(_original.Artifacts).All(pair => pair.First.Bytes.SequenceEqual(pair.Second.Bytes)).ShouldBeTrue();
}
