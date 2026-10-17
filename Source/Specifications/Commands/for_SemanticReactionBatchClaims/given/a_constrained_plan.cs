// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Stage.Specifications.Commands.for_SemanticReactionBatchClaims.given;

public class a_constrained_plan : Specification
{
    const string Source = """
        module Work
          feature Items
            slice Automation Process
              event Changed
                value String
              event Claimed
              event Released
              constraint UniqueValue
                unique value on Changed
                message "Already held"
              constraint OneClaim
                unique event Claimed
                released by Released
                message "Already claimed"
        """;

    protected SemanticExecutionPlan _plan = null!;
    protected SemanticConstraint?[] _results = null!;

    void Establish()
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Claims"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("claims"), "claims", "Claims.play", Source);
        var compilation = new SemanticModelCompiler().Compile("Claims", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join(Environment.NewLine, compilation.Diagnostics));
        _plan = SemanticExecutionPlan.Compile(compilation.Value!.Model).Plan!;
    }

    internal SemanticConstraintEvaluator.Fact Changed(string owner, string value)
    {
        var contract = _plan.Events.Values.Single(candidate => candidate.Name == "Changed");

        return new(contract.Id, SemanticValue.Text(owner), [new(contract.Properties.Single().Id, SemanticValue.Text(value))]);
    }

    internal SemanticConstraintEvaluator.Fact Claimed(string owner) => Bare("Claimed", owner);

    internal SemanticConstraintEvaluator.Fact Released(string owner) => Bare("Released", owner);

    internal SemanticConstraintEvaluator.Fact Bare(string name, string owner) => new(_plan.Events.Values.Single(candidate => candidate.Name == name).Id, SemanticValue.Text(owner), []);

    internal void Validate(IReadOnlyList<SemanticConstraintEvaluator.Fact> history, params SemanticConstraintEvaluator.Fact[] candidates)
    {
        var claims = new SemanticReactionBatchClaims(_plan, history);
        _results = [.. candidates.Select(claims.Validate)];
    }
}
