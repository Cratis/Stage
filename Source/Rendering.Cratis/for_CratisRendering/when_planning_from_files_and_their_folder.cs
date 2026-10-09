// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRendering;

public class when_planning_from_files_and_their_folder : given.a_source_plan
{
    CratisPlanResult _folder = null!;
    async Task Establish() => _folder = await From(".");
    async Task Because() => _result = await From("projects.play", ".", "tasks.play");
    [Fact] void should_plan_successfully() => ShouldSucceed();
    [Fact] void should_deduplicate_sources() => _result.Digest.ShouldEqual(_folder.Digest);
    [Fact] void should_preserve_the_revision() => _result.Revision.ShouldEqual(_folder.Revision);
}
