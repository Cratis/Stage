// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Specifications.Commands;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor;

public class when_rejecting_non_canonical_uuid_inputs : Specification
{
    [Theory]
    [InlineData("A1111111-1111-4111-8111-111111111111")]
    [InlineData("{a1111111-1111-4111-8111-111111111111}")]
    [InlineData("a1111111111141118111111111111111")]
    public void should_reject_before_the_requirement_runs(string requestedBy)
    {
        const string source = """
            concept MemberId : Uuid
            module Access
              feature Approvals
                slice StateChange Approve
                  command ApproveRequest
                    id String identifier
                    requestedBy MemberId
                    approvedBy MemberId
                    validate
                      require approvedBy != requestedBy
                        message "Approver must differ from requester."
                    produces RequestApproved
                      for id
                  event RequestApproved
            """;

        // Specification fixtures cannot carry a non-canonical Uuid (the binder refuses it), so the request is built directly.
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("UuidInputs"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("app"), "app", "app.play", source);
        var compilation = new SemanticModelCompiler().Compile("UuidInputs", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join(';', compilation.Diagnostics));
        var plan = SemanticExecutionPlan.Compile(compilation.Value!.Model).Plan!;
        var command = plan.Commands.Values.Single();
        var values = command.Properties.Select(property => new SemanticPropertyValue(property.Id, SemanticValue.Text(property.Name switch
        {
            "id" => "request",
            "requestedBy" => requestedBy,
            _ => "b2222222-2222-4222-8222-222222222222"
        }))).ToImmutableArray();
        var reference = new SemanticEvaluator().Execute(plan, SemanticWorld.Empty, SemanticExecutionRequest.Create(command.Id, values, []));
        var rejection = Assert.IsType<SemanticRejected>(reference);
        rejection.World.Facts.ShouldBeEmpty();
        rejection.Details.ShouldNotEqual("Approver must differ from requester.");
        SemanticConceptValues.RequestFailure(plan, command, values).ShouldEqual(rejection.Details);
    }
}
