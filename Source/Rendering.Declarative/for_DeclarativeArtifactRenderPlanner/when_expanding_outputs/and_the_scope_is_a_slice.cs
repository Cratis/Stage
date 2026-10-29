// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Declarative.for_DeclarativeArtifactRenderPlanner.when_expanding_outputs;

public class and_the_scope_is_a_slice : given.an_expansion
{
    DeclarativeOutputExpansion _expansion = null!;

    void Because()
    {
        var request = Request(Profile(Archive()));
        var reply = request.Model.Application.Modules[0].Features[0].Slices.Single(_ => _.Name == "Reply");
        _expansion = Expand(Scoped(request, ArtifactRenderScopeKind.Slice, reply.Id));
    }

    [Fact] void should_render_the_commands_of_the_slice() => Names(_expansion).Count(_ => _.StartsWith("command:", StringComparison.Ordinal)).ShouldEqual(2);
    [Fact] void should_not_render_constructs_of_other_slices() => Names(_expansion).Any(_ => _.StartsWith("reaction:", StringComparison.Ordinal)).ShouldBeFalse();
}
