// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticReactionSpecificationRenderer.given;

/// <summary>
/// A reaction acknowledging the refusal of a command another slice owns, whose specification states both the invocation and that nothing is appended.
/// </summary>
public class a_refusal_outcome_with_invocation_expectations : a_reaction_specification
{
    internal const string Source = """
        module Work
          feature Processing
            slice StateChange Completion
              command Complete
                id String identifier
                validate
                  require id != "blocked"
                    message "blocked"
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
              reaction Worker
                when Started
                  invokes Complete
                    id = id
                    on refused
                      acknowledge
              specification Silent
                when append Started
                  for "blocked"
                  id = "blocked"
                then no events
        """;

    void Establish()
    {
        Prepare(Source);
        SetExpectations("blocked");
    }

    private protected void SetExpectations(string id)
    {
        var command = _context.Commands.Values.Single(candidate => candidate.Name == "Complete");
        _specification = _specification with { ThenInvocations = [new(command.Id, [new(command.Properties.Single().Id, SemanticValue.Text(id))])] };
    }

    protected void ValidateExpectations() => SemanticSpecificationAdmission.ValidateInvocations(_context, _located.Slice with { Specifications = [_specification] }, _diagnostics);
}
#endif
