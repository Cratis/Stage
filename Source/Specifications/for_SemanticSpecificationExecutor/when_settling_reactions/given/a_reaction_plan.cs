// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_settling_reactions.given;

public class a_reaction_plan : Specification
{
    protected SemanticExecutionPlan _plan = null!;
    protected SemanticSpecificationRunRecord _result = null!;
    protected SemanticSpecificationRun _reference = null!;

    protected void Compile(string reaction, string expected, ReactionScenario? scenario = null)
    {
        scenario ??= new ReactionScenario();
        var (action, policy, authorization, commandProperties, declarations, caller, eventSources, commandDestination) = scenario;
        var source = $"""
            {policy}
            concept Token : Uuid
            {eventSources}
            module Work
              feature Items
                slice Automation Process
                  command Begin
                    item String identifier
                    amount Decimal
                    produces Started
                      for item
                      item = item
                      amount = amount
                  command Complete
                    item String identifier
            {Indent(commandProperties, 8)}
            {Indent(authorization, 8)}
                    produces Completed
            {Indent(commandDestination, 10)}
                  event Started
                    item String
                    amount Decimal
                  event Completed
                  event Followed
                    item String
                  event Noted
                    notedAt DateTime
                  readmodel Marker
                    item String key
                  projection Markers => Marker
                    from Completed
                      item = $eventSourceId
            {Indent(declarations, 6)}
            {Indent(reaction, 6)}
                  specification Exercising
            {Indent(caller, 8)}
            {Indent(action, 8)}
            {Indent(expected.Length == 0 ? "then no readmodel Marker for \"one\"" : expected, 8)}
            """;
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Reactions"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("reaction"), "reaction", "Reaction.play", source);
        var compilation = new SemanticModelCompiler().Compile("Reactions", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join(Environment.NewLine, compilation.Diagnostics));
        var compiled = SemanticExecutionPlan.Compile(compilation.Value!.Model);
        Assert.NotNull(compiled.Plan);
        _plan = compiled.Plan!;
    }

    protected async Task Execute()
    {
        var specification = _plan.Specifications.Values.Single();
        _reference = new SemanticSpecificationRunner().Run(_plan, specification.Id);
        _result = (await new SemanticSpecificationExecutor().Run(_plan, new([specification.Id]), new())).Results.Single();
    }

    protected void AssertMatchesReference()
    {
        if (_reference.Execution is not SemanticUnsupported) Assert.True(_reference.Passed, string.Join(';', _reference.Failures));
        Assert.True(_result.Outcome == (_reference.Execution is SemanticUnsupported ? SemanticSpecificationOutcome.Unsupported : SemanticSpecificationOutcome.Passed), $"{_result.Outcome}: {_result.Unsupported?.Details} {string.Join(';', _result.Failures)}");
        if (_reference.Execution is not SemanticUnsupported) _result.ExecutionKind.ShouldEqual(_reference.Execution.Kind.ToString());
    }

    static string Indent(string text, int spaces) => string.Join('\n', text.Split('\n').Select(line => new string(' ', spaces) + line));
}

/// <summary>
/// The optional parts of a reaction scenario around its reaction and expectation.
/// </summary>
/// <param name="Action">The specification's action.</param>
/// <param name="Policy">The policy declarations.</param>
/// <param name="Authorization">The authorization clause of the command that completes.</param>
/// <param name="CommandProperties">Extra properties of the command that completes.</param>
/// <param name="Declarations">Extra slice declarations, such as constraints.</param>
/// <param name="Caller">The specification's given caller.</param>
/// <param name="EventSources">The event source declarations.</param>
/// <param name="CommandDestination">The destination clause of the completing production.</param>
public record ReactionScenario(
    string Action = "when append Started\n  for \"one\"\n  item = \"one\"\n  amount = 1",
    string Policy = "",
    string Authorization = "",
    string CommandProperties = "",
    string Declarations = "",
    string Caller = "",
    string EventSources = "",
    string CommandDestination = "for item");
