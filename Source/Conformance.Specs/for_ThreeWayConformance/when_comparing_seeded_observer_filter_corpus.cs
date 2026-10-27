// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Specifications;
using Xunit;

namespace Cratis.Stage.Conformance.Specs.for_ThreeWayConformance;

public class when_comparing_seeded_observer_filter_corpus : Specification
{
    [Theory]
    [InlineData("seed")]
    [InlineData("unrouted")]
    [InlineData("matching")]
    [InlineData("query")]
    public async Task should_preserve_seeded_reference_parity_without_rendering_bodies(string path)
    {
        var plan = Compile(path);
        var specification = plan.Specifications.Values.Single();
        var reference = new SemanticSpecificationRunner().Run(plan, specification.Id);
        var result = (await new SemanticSpecificationExecutor().Run(plan, new([specification.Id]), new())).Results.Single();
        reference.Execution.Kind.ShouldEqual(SemanticExecutionOutcomeKind.Unsupported);
        result.Outcome.ShouldEqual(SemanticSpecificationOutcome.Unsupported);
        result.Unsupported!.Details.ShouldContain("STAGE-IMPL-003:");
    }

    static SemanticExecutionPlan Compile(string path)
    {
        if (path == "query") return CompileQuery();
        var model = SemanticModelSerializer.Deserialize(EventRoutesCorpus.ObserverFiltersV10.EsmBytes.AsSpan());
        var module = model.Application.Modules.Single();
        var feature = module.Features.Single();
        var slice = feature.Slices.Single();
        var specification = slice.Specifications.Single(specification => specification.Name == (path == "matching" ? "MatchingReducer" : "UnroutedReducer"));
        var readModel = slice.ReadModels.Single(readModel => readModel.Name == "Snapshot");
        var property = readModel.Properties.Single();
        property.Name.ShouldEqual("id");
        var key = SemanticValue.Text("seed");
        var state = new SemanticSpecificationReadModel(readModel.Id, key, [new(property.Id, key)]);
        var idle = path == "seed";
        specification = specification with
        {
            WhenAppended = idle ? null : specification.WhenAppended,
            ThenEvents = idle ? [] : specification.ThenEvents,
            GivenReadModels = [state],
            ThenReadModels = [state]
        };
        slice = slice with { Specifications = [specification] };
        var application = model.Application with { Modules = [module with { Features = [feature with { Slices = [slice] }] }] };
        return SemanticExecutionPlan.Compile(ExecutableSemanticModel.Create(model.LanguageVersion, model.SemanticVersion, application)).Plan!;
    }

    static SemanticExecutionPlan CompileQuery()
    {
        const string source = """
            eventsource Account
              stream Notes
            module M
              feature F
                slice Automation S
                  event Folded
                  command Fold
                    id String identifier
                    stream Account.Notes
                    produces Folded
                      for id
                  readmodel Snapshot
                    id String key
                  reducer Fold => Snapshot
                    from Account
                    on Folded
                      ```csharp
                      return context.State;
                      ```
                  query SnapshotById => Snapshot optional
                    by id String
                  specification SeededQuery
                    given readmodel Snapshot
                      id = "seed"
                    then query SnapshotById
                      arguments
                        id = "seed"
                      result
                        id = "seed"
            """;
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("SeededReducerQuery"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("seeded-query"), "seeded-query", "seeded-query.play", source);
        var compiled = new SemanticModelCompiler().Compile("SeededReducerQuery", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compiled.Success, string.Join(';', compiled.Diagnostics));
        var plan = SemanticExecutionPlan.Compile(compiled.Value!.Model).Plan!;
        plan.Specifications.Values.Single().ThenQueries.Single().Query.ShouldEqual(plan.Queries.Values.Single().Id);
        return plan;
    }
}
