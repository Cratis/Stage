// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticOwnerOmission.when_computing_a_closure;

public class reaching_an_enclosing_owner_through_compute : given.an_omission_context
{
    SemanticOwnerOmission? _omission;

    // Alias the enclosing identity to a reachable declaration to exercise Compute's application-wide refusal.
    void Establish() => _application = _application with { Id = _registerProject.Id };

    void Because()
    {
        BuildContext();
        _omission = SemanticOwnerOmission.Compute(_context, _index, Roots(_registerProject.Events.Single().Id));
    }

    [Fact] void should_have_a_complete_index() => _index.IsSemanticGraphComplete.ShouldBeTrue();
    [Fact] void should_reach_the_application() => _index.Dependents(_registerProject.Events.Single().Id).ShouldContain(_application.Id);
    [Fact] void should_not_be_provable() => _omission.ShouldBeNull();
}
