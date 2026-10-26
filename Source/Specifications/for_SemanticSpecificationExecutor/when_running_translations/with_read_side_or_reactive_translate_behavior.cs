// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_translations;

public class with_read_side_or_reactive_translate_behavior
{
    [Theory]
    [InlineData("reaction", true, false)]
    [InlineData("reaction", true, true)]
    [InlineData("reaction", false, false)]
    [InlineData("reaction", false, true)]
    [InlineData("projection", true, false)]
    [InlineData("projection", true, true)]
    [InlineData("projection", false, false)]
    [InlineData("projection", false, true)]
    [InlineData("reducer", true, false)]
    [InlineData("reducer", true, true)]
    [InlineData("reducer", false, false)]
    [InlineData("reducer", false, true)]
    public async Task should_preserve_the_reachable_translate_boundary(string kind, bool reachable, bool withinTranslate)
    {
        var source = """
            module Work
              feature Work
                slice StateChange Work
                  event Saved
                  event Historical
                  event Followed
                  command Save
                    id String identifier
                    produces Saved
                      for id
                  specification Saving
                    when Save
                      id = "one"
                    then Saved
                      for "one"
                slice Translate Boundary
                  readmodel Snapshot
                    id String key
            """;
        var behavior = kind switch
        {
            "reaction" => """
                reaction Follow
                  when SOURCE
                    produces Followed
                      for "one"
                """,
            "projection" => """
                projection Build => Snapshot
                  from SOURCE
                    id = $eventSourceId
                """,
            _ => """
                reducer Fold => Snapshot
                  on SOURCE
                    ```csharp
                    return context.State;
                    ```
                """
        };
        source += "\n" + string.Join('\n', behavior.Replace("SOURCE", reachable ? "Saved" : "Historical", StringComparison.Ordinal).Split('\n').Select(line => "      " + line));
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Work"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("work"), "work", "Work.play", source);
        var compilation = new SemanticModelCompiler().Compile("Work", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join(';', compilation.Diagnostics));
        var model = compilation.Value!.Model;
        if (withinTranslate)
        {
            var module = model.Application.Modules.Single();
            var feature = module.Features.Single();
            var specifications = feature.Slices.Single(slice => slice.Kind == SemanticSliceKind.StateChange).Specifications;
            var slices = feature.Slices.Select(slice => slice with { Specifications = slice.Kind == SemanticSliceKind.Translate ? specifications : [] }).ToArray();
            var application = model.Application with { Modules = [module with { Features = [feature with { Slices = [.. slices] }] }] };
            model = ExecutableSemanticModel.Create(model.LanguageVersion, model.SemanticVersion, application);
        }
        var plan = SemanticExecutionPlan.Compile(model).Plan!;
        var result = (await new SemanticSpecificationExecutor().Run(plan, new([]), new())).Results.Single();

        // Screenplay decision 0115: another slice's reaction does not run for this specification, so only the Translate slice's own
        // specification reaches its reaction; read-model projections and reducers still build what any specification reads.
        var unsupported = kind == "reaction" ? withinTranslate : reachable;
        result.Outcome.ShouldEqual(unsupported ? SemanticSpecificationOutcome.Unsupported : SemanticSpecificationOutcome.Passed);
        if (unsupported) result.Unsupported!.Details.StartsWith("STAGE-ESM-024:", StringComparison.Ordinal).ShouldBeTrue();
        else result.Unsupported.ShouldBeNull();
    }
}
