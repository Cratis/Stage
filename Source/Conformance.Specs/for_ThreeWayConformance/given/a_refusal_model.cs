// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics;
using Xunit;

namespace Cratis.Stage.Conformance.Specs.for_ThreeWayConformance.given;

internal static class a_refusal_model
{
    internal const string Source = """
        module Work
          feature Processing
            slice Automation Handling
              event Requested
                id String
                amount Decimal
              event QuietRequested
                id String
                amount Decimal
              event Completed
              event Recorded
                reason String
                message String
              event Skipped
              command Begin
                id String identifier
                amount Decimal
                produces Requested
                  for id
                  id = id
                  amount = amount
                produces QuietRequested
                  for id
                  id = id
                  amount = amount
              command Complete
                id String identifier
                amount Decimal
                validate
                  amount > 0 message " first "
                  require amount != 0
                    message "second"
                produces Completed
                  for id
              command FollowUp
                id String identifier
                produces Skipped
                  for id
              reaction Record
                when Requested
                  invokes Complete
                    id = id
                    amount = amount
                    on refused by validation
                      produces Recorded
                        reason = $refusal.reason
                        message = $refusal.message
                    on refused
                      acknowledge
                  invokes FollowUp
                    id = id
              reaction Ignore
                when QuietRequested
                  invokes Complete
                    id = id
                    amount = amount
                    on refused
                      acknowledge
                  invokes FollowUp
                    id = id
              specification RecordValidation
                when append Requested
                  for "one"
                  id = "one"
                  amount = 0
                then Recorded
                  for "one"
                  reason = "validation"
                  message = " first \nsecond"
              specification AcknowledgeValidation
                when append QuietRequested
                  for "one"
                  id = "one"
                  amount = 0
                then no events
        """;

    internal static ExecutableSemanticModel Compile(string source = Source)
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Refusals"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("refusals"), "refusals", "Refusals.play", source);
        var compilation = new SemanticModelCompiler().Compile("Refusals", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join(Environment.NewLine, compilation.Diagnostics));

        return compilation.Value!.Model;
    }
}
#endif
