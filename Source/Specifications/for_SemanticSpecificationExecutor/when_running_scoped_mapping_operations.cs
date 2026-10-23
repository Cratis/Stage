// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Semantics;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor;

public class when_running_scoped_mapping_operations : Specification
{
    [Theory]
    [InlineData("set", "4", "2.5", "approved", true)]
    [InlineData("add", "12", "12", "draft", true)]
    [InlineData("concept-add", "12", "12", "draft", true)]
    [InlineData("subtract", "8", "9", "draft", true)]
    [InlineData("increment", "11", "11.5", "draft", true)]
    [InlineData("decrement", "9", "9.5", "draft", true)]
    [InlineData("zero", "0", "0", "draft", true)]
    [InlineData("every", "2", "1.5", "fixed", true)]
    [InlineData("ordered", "16", "15", "draft", true)]
    [InlineData("product", "14", "12.75", "draft", true)]
    [InlineData("product", "13", "12.75", "draft", false)]
    [InlineData("set", "999", "2.5", "approved", false)]
    public async Task should_match_the_reference_without_leaking_between_runs(string operation, string whole, string amount, string state, bool passes)
    {
        var plan = Compile(operation, whole, amount, state);
        var reference = new SemanticSpecificationRunner().Run(plan, plan.Specifications.Keys.Single());
        reference.Passed.ShouldEqual(passes);
        var executor = new SemanticSpecificationExecutor();
        for (var run = 0; run < 2; run++)
        {
            var result = Assert.Single((await executor.Run(plan, new([]), new())).Results);
            Assert.True(result.Outcome == (passes ? SemanticSpecificationOutcome.Passed : SemanticSpecificationOutcome.Failed),
                $"{result.Outcome}: {result.Unsupported?.Details} {string.Join(';', result.Failures)}");
            result.Failures.ShouldContainOnly(reference.Failures);
            result.Trace!.ReadModels.Count.ShouldEqual(1);
        }
    }

    [Theory]
    [InlineData("add whole by 3")]
    public void should_keep_unsupported_arithmetic_refused(string mapping)
    {
        var plan = Compile("add", "12", "12", "draft", mapping);
        SemanticRunAdmission.Check(plan, plan.Specifications.Values.Single())!.Capability.ShouldEqual(StageExecutionCapability.Projection);
    }

    internal static SemanticExecutionPlan Compile(string operation, string whole, string amount, string state, string? overrideMapping = null)
    {
        var mapping = overrideMapping ?? operation switch
        {
            "set" => "state = \"approved\"\nwhole = 4\namount = 2.5\nactive = true",
            "every" => "state = state\nwhole = whole\namount = amount",
            "product" => "add whole by whole * whole\nadd amount by amount * amount",
            "zero" => "add whole by 0\nsubtract amount by 0",
            "increment" or "decrement" => $"{operation} whole\n{operation} amount",
            _ => $"{(operation == "ordered" || operation == "concept-add" ? "add" : operation)} whole by whole\n{(operation == "ordered" || operation == "concept-add" ? "add" : operation)} amount by amount"
        };
        var initialization = operation == "zero" ? "add whole by 0\nsubtract amount by 0" : "whole = 10\namount = 10.5";
        var preceding = operation == "ordered" ? "given Changed\n  for \"one\"\n  state = \"ignored\"\n  whole = 2\n  amount = 1.5\ngiven Changed\n  for \"one\"\n  state = \"ignored\"\n  whole = 2\n  amount = 1.5" : string.Empty;
        var source = $$"""
            module Work
              feature Work
                slice StateChange Work
                  event Initialized
                  event Changed
                    state String
                    whole Int
                    amount Decimal
                  command Initialize
                    id String identifier
                    produces Initialized
                      for id
                  command Change
                    id String identifier
                    state String
                    whole Int
                    amount Decimal
                    produces Changed
                      for id
                      state = state
                      whole = whole
                      amount = amount
                  specification MappingOperations
                    given Initialized
                      for "one"
            {{Indent(preceding, 8)}}
                    when append Changed
                      for "one"
                      state = "ignored"
                      whole = 2
                      amount = 1.5
                    then readmodel Current
                      id = "one"
                      state = "{{state}}"
                      whole = {{whole}}
                      amount = {{amount}}
                      active = {{(operation == "set" ? "true" : "false")}}
                slice StateView Current
                  readmodel Current
                    id String key
                    state String
                    whole Int
                    amount Decimal
                    active Bool
                  projection Build => Current
                    no automap
                    from Initialized
                      id = $eventSourceId
                      state = "draft"
                      active = false
            {{Indent(initialization, 10)}}
                    from Changed
            {{Indent(mapping, 10)}}
            {{(operation == "every" ? "        every\n          exclude children\n          state = \"fixed\"" : string.Empty)}}
            """;
        if (operation == "concept-add")
        {
            source = source.Replace("module Work", "concept Count : Int\nconcept Amount : Decimal\nmodule Work", StringComparison.Ordinal)
                .Replace("whole Int", "whole Count", StringComparison.Ordinal).Replace("amount Decimal", "amount Amount", StringComparison.Ordinal);
        }
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("MappingOperations"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("model"), "model", "Model.play", source);
        var compilation = new SemanticModelCompiler().Compile("MappingOperations", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join(';', compilation.Diagnostics));
        return SemanticExecutionPlan.Compile(compilation.Value!.Model).Plan!;
    }

    static string Indent(string text, int spaces) => string.Join('\n', text.Split('\n').Select(line => new string(' ', spaces) + line));
}
