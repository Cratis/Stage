// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Semantics;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_flat_transitions.given;

public class a_flat_projection : Specification
{
    protected SemanticExecutionPlan _plan = null!;
    protected SemanticSpecificationRunRecord[] _results = [];
    protected bool _referencePassed;
    protected string _referenceFailures = string.Empty;

    /// <summary>
    /// Compiles a model whose projection body and specification are supplied, runs the reference and the executor twice.
    /// </summary>
    /// <param name="projectionBody">The indented lines that follow <c>projection Rows =&gt; Row</c>.</param>
    /// <param name="givens">The indented given lines.</param>
    /// <param name="expected">The indented read model assertion lines.</param>
    protected async Task Run(string projectionBody, string givens, string expected)
    {
        var source = $$"""
            module Work
              feature Items
                slice StateView Rows
                  event Claimed
                    alias String
                    owner String
                  event Released
                    alias String
                    note String
                  command Claim
                    id String identifier
                    alias String
                    owner String
                    produces Claimed
                      for id
                      alias = alias
                      owner = owner
                  command Release
                    id String identifier
                    alias String
                    note String
                    produces Released
                      for id
                      alias = alias
                      note = note
                  readmodel Row
                    id String key
                    owner String optional
                    note String optional
                    kind String optional
                  projection Rows => Row
                    no automap
            {{Indent(projectionBody, 8)}}
                  specification Checking
            {{Indent(givens, 8)}}
                    then readmodel Row
            {{Indent(expected, 10)}}
            """;
        _plan = when_running_property_keyed_removal_only_projections.Compile(source);
        var id = _plan.Specifications.Keys.Single();
        var reference = new SemanticSpecificationRunner().Run(_plan, id);
        _referencePassed = reference.Passed;
        _referenceFailures = string.Join(';', reference.Failures);
        var executor = new SemanticSpecificationExecutor();
        _results = [.. (await executor.Run(_plan, new([]), new())).Results, .. (await executor.Run(_plan, new([]), new())).Results];
    }

    protected void AssertPlanIsFlat() =>
        Assert.True(_plan.Projections.Values.Single().Scope is null, "The projection must stay flat in the plan.");

    protected void AssertParity(bool passes)
    {
        Assert.True(_referencePassed == passes, $"Reference passed {_referencePassed}: {_referenceFailures}");
        var admission = SemanticRunAdmission.Check(_plan, _plan.Specifications.Values.Single());
        Assert.True(admission is null, admission?.Details);
        foreach (var result in _results)
        {
            Assert.True(result.Outcome == (passes ? SemanticSpecificationOutcome.Passed : SemanticSpecificationOutcome.Failed),
                $"{result.Outcome}: {result.Unsupported?.Details} {string.Join(';', result.Failures)}");
        }
    }

    static string Indent(string text, int spaces) =>
        string.Join('\n', text.Split('\n').Select(line => new string(' ', spaces) + line));
}
