// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_translations;

// Screenplay decision 0115: the Automation slice's specification runs only its own slice, so the outbound Translate slice's
// publication does not run between the reaction's appends; the appends keep the reference's append or transaction boundary.
public class with_reaction_boundaries_beside_another_slices_publication : Specification
{
    [Theory]
    [InlineData("legacy", false)]
    [InlineData("legacy", true)]
    [InlineData("atomic", false)]
    [InlineData("atomic", true)]
    [InlineData("commands", false)]
    [InlineData("commands", true)]
    public async Task should_append_at_the_same_boundary_as_the_reference_without_publishing(string mode, bool rejectSecond)
    {
        var plan = Compile(mode, rejectSecond);
        var specification = plan.Specifications.Values.Single();
        var reference = new SemanticSpecificationRunner().Run(plan, specification.Id);
        Assert.True(reference.Passed, string.Join(';', reference.Failures));
        var count = 0;
        var executor = new SemanticSpecificationExecutor { ObserveWorldFacts = (_, facts) => count = facts };
        var actual = (await executor.Run(plan, new([specification.Id]), new())).Results.Single();
        Assert.True(actual.Outcome == SemanticSpecificationOutcome.Passed, $"{actual.Unsupported?.Details} {string.Join(';', actual.Failures)}");
        actual.ExecutionKind.ShouldEqual(reference.Execution.Kind.ToString());
        count.ShouldEqual(reference.Execution.World.Facts.Length);
        if (!rejectSecond)
        {
            var accepted = (SemanticAccepted)reference.Execution;
            actual.Trace!.Facts.Select(fact => fact.EventContract).ShouldEqual(accepted.Facts.Select(fact => fact.EventContract.ToString()));
            var names = accepted.Facts.Select(fact => plan.Events[fact.EventContract].Name).ToArray();
            names.ShouldEqual(["Started", "First", "Second"]);
        }
        else
        {
            count.ShouldEqual(mode == "atomic" ? 2 : 3);
        }
    }

    static SemanticExecutionPlan Compile(string mode, bool rejectSecond)
    {
        var effects = mode == "commands"
            ? "invokes EmitFirst\n  id = \"one\"\ninvokes EmitSecond\n  id = \"one\""
            : "produces First\n  for \"one\"\nproduces Second\n  for \"one\"";
        var expected = rejectSecond ? "then error \"Already second\"" : "then First\n  for \"one\"\nthen Second\n  for \"one\"";
        var source = $$"""
            {{(mode == "atomic" ? "reaction batches atomic" : "")}}
            module Work
              feature Work
                slice Automation Work
                  event Started
                  event First
                  event Second
                  command Begin
                    id String identifier
                    produces Started
                      for id
                  command EmitFirst
                    id String identifier
                    produces First
                      for id
                  command EmitSecond
                    id String identifier
                    produces Second
                      for id
                  constraint Once
                    unique event Second
                    message "Already second"
                  reaction Follow
                    when Started
            {{Indent(effects, 10)}}
                  specification Exercising
            {{(rejectSecond ? "        given Second\n          for \"one\"" : "")}}
                    when append Started
                      for "one"
            {{Indent(expected, 8)}}
                slice Translate Publish
                  direction outbound
                  public event Published
                    status String
                  projection PublishFirst => Published
                    from First
                      status = "published"
            """;
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Boundaries"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("boundaries"), "boundaries", "Boundaries.play", source);
        var compilation = new SemanticModelCompiler().Compile("Boundaries", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join(';', compilation.Diagnostics));
        return SemanticExecutionPlan.Compile(compilation.Value!.Model).Plan!;
    }

    static string Indent(string text, int spaces) => string.Join('\n', text.Split('\n').Select(line => new string(' ', spaces) + line));
}
