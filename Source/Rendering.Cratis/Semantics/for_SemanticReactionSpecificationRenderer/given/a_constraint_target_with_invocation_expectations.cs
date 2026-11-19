// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticReactionSpecificationRenderer.given;

/// <summary>
/// A reaction whose triggering event is a unique constraint target and which invokes a command another slice owns, with a specification that states only the invocation.
/// </summary>
public class a_constraint_target_with_invocation_expectations : a_reaction_specification
{
    internal const string Source = """
        module Work
          feature Processing
            slice StateChange Completion
              command Complete
                id String identifier
                produces Completed
                  for id
              event Completed
            slice Automation FollowUp
              command Start
                id String identifier
                produces Started
                  for id
                  id = id
              event Started
                id String
              constraint UniqueStarted
                unique id on Started
              reaction Worker
                when Started
                  invokes Complete
                    id = id
              specification Recorded
                when append Started
                  for "fine"
                  id = "fine"
                then no events
        """;

    void Establish()
    {
        Prepare(Source);
        var command = _context.Commands.Values.Single(candidate => candidate.Name == "Complete");
        _specification = _specification with { ThenNoEvents = false, ThenInvocations = [new(command.Id, [new(command.Properties.Single().Id, SemanticValue.Text("fine"))])] };
    }

    protected void ValidateExpectations() => SemanticSpecificationAdmission.ValidateInvocations(_context, _located.Slice with { Specifications = [_specification] }, _diagnostics);
}
#endif
