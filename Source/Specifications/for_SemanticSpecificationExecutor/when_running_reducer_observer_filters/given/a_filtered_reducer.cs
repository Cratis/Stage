// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_reducer_observer_filters.given;

public class a_filtered_reducer : Specification
{
    protected static SemanticExecutionPlan Plan(string path, string route, bool seeded, bool constrained = false)
    {
        var model = Compile();
        var module = model.Application.Modules.Single();
        var feature = module.Features.Single();
        var slice = feature.Slices.Single();
        var specification = Seed(slice, seeded);
        var fact = specification.ThenEvents.Single() with { Route = Route(model.Application, route) };
        specification = WithAction(specification, slice, fact, path) with
        {
            ThenAbsentReadModels = path == "given" && !seeded ? [new(slice.ReadModels.Single().Id, SemanticValue.Text("one"))] : []
        };
        var command = slice.Commands.Single();
        var fixture = fact.Route;
        command = command with { Route = fixture is null ? null : new(fixture.Source, fixture.Stream) };
        if (constrained)
        {
            command = command with { Produces = [command.Produces.Single(), command.Produces.Single()] };
            specification = specification with { ThenEvents = [], ThenReadModels = [], ThenErrors = [new("Once", "Already changed")] };
        }
        slice = slice with
        {
            Commands = [command],
            Specifications = [specification],
            Constraints = constrained ? [new("Once", SemanticConstraintKind.UniqueEventOccurrence, SemanticConstraintScope.EventSequence, [new(fact.EventContract, [])], [], false, "Already changed")] : []
        };
        var application = model.Application with { Modules = [module with { Features = [feature with { Slices = [slice] }] }] };
        return SemanticExecutionPlan.Compile(ExecutableSemanticModel.Create(model.LanguageVersion, model.SemanticVersion, application)).Plan!;
    }

    static SemanticSpecification WithAction(SemanticSpecification specification, SemanticSlice slice, SemanticSpecificationEvent fact, string path) => specification with
    {
        When = path == "command" ? specification.When : null,
        ThenEvents = path == "command" || path == "append" ? [fact] : [],
        GivenEvents = path == "given" ? [fact] : [],
        WhenAppended = path == "append" ? new(fact.EventContract, fact.Values) { EventSource = fact.EventSource, Route = fact.Route } : null,
        ThenQueries = path == "query" ? [new(slice.Queries.Single().Id, SemanticValue.Text("one"), [])] : []
    };

    static SemanticSpecification Seed(SemanticSlice slice, bool seeded)
    {
        var specification = slice.Specifications.Single();
        var key = SemanticValue.Text("one");
        var state = new SemanticSpecificationReadModel(slice.ReadModels.Single().Id, key, [new(slice.ReadModels.Single().Properties.Single().Id, key)]);
        return seeded ? specification with { GivenReadModels = [state], ThenReadModels = [state] } : specification;
    }

    static SemanticFixtureRoute? Route(SemanticApplication application, string route)
    {
        if (route == "none") return null;
        var source = application.EventSources.Single(source => source.Name == (route == "source" ? "Other" : "Account"));
        return new(source.Id, source.Streams.Single(stream => stream.Name == (route == "stream" ? "Archive" : "Notes")).Id);
    }

    static ExecutableSemanticModel Compile()
    {
        const string source = """
            eventsource Account
              stream Notes
              stream Archive
            eventsource Other
              stream Notes
            module Work
              feature Items
                slice StateChange Manage
                  command Feed
                    id String identifier
                    produces Changed
                      for id
                  event Changed
                  readmodel Snapshot
                    id String key
                  query SnapshotById => Snapshot?
                    by id String
                  reducer Fold => Snapshot
                    from Account.Notes
                    on Changed
                      ```csharp
                      throw new InvalidOperationException("Never execute an authored body");
                      ```
                  specification Check
                    when Feed
                      for "one"
                      id = "one"
                    then Changed
                      for "one"
            """;
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("FilteredReducer"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("app"), "app", "app.play", source);
        var compiled = new SemanticModelCompiler().Compile("Filters", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compiled.Success, string.Join(';', compiled.Diagnostics));
        return compiled.Value!.Model;
    }
}
