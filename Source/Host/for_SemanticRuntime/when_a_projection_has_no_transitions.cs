// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Host.for_SemanticRuntime.given;
using Cratis.Stage.Semantics;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntime;

public class when_a_projection_has_no_transitions : a_semantic_runtime
{
    bool _mirrored;
    string _reason = string.Empty;

    void Because() => _mirrored = SemanticProjectionMirrors.TryLower(
        _runtime.Plan,
        _runtime.Plan.Projections.Values.Single() with { Transitions = [] },
        out _,
        out _,
        out _reason);

    [Fact] void should_not_register_a_lossy_mirror() => _mirrored.ShouldBeFalse();
    [Fact] void should_explain_the_refusal() => _reason.ShouldNotBeEmpty();
}
