// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Semantics;
using Cratis.Stage.Host.for_SemanticWorldRebuilder.given;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticWorldRebuilder.when_rebuilding;

public class with_an_unmirrored_projection : a_rebuildable_world
{
    bool _mirrored;

    void Because()
    {
        var projection = _plan.Projections.Values.Single() with { Transitions = [] };
        _mirrored = SemanticProjectionMirrors.TryLower(_plan, projection, out _, out _, out _);
    }

    [Fact] void should_refuse_the_projection() => _mirrored.ShouldBeFalse();
}
