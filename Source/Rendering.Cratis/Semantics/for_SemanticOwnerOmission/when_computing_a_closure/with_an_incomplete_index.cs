// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticOwnerOmission.when_computing_a_closure;

public class with_an_incomplete_index : given.an_omission_context
{
    SemanticOwnerOmission? _omission;

    void Establish()
    {
        var command = _registerProject.Commands.Single();
        _registerProject = _registerProject with
        {
            Commands = [command with { Produces = [command.Produces.Single() with { EventContract = Identity("missing-event") }] }]
        };
    }

    void Because()
    {
        BuildContext();
        _omission = SemanticOwnerOmission.Compute(_context, _index, Roots(_projectLookup.Queries.Single().Id));
    }

    [Fact] void should_have_an_incomplete_index() => _index.IsSemanticGraphComplete.ShouldBeFalse();
    [Fact] void should_not_be_provable() => _omission.ShouldBeNull();
}
