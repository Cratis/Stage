// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticOwnerOmission.when_computing_a_closure;

public class with_an_authored_scene_and_an_omitted_command : given.an_omission_context
{
    SemanticOwnerOmission? _omission;
    SemanticOwnerOmission? _withoutScene;

    void Because()
    {
        BuildContext(WithAuthoredScene());
        _omission = SemanticOwnerOmission.Compute(_context, _index, Roots(_registerProject.Events.Single().Id));
        BuildContext();
        _withoutScene = SemanticOwnerOmission.Compute(_context, _index, Roots(_registerProject.Events.Single().Id));
    }

    [Fact] void should_not_be_provable() => _omission.ShouldBeNull();
    [Fact] void should_be_provable_for_the_same_roots_without_an_authored_scene() => _withoutScene.ShouldNotBeNull();
}
