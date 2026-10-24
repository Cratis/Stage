// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Specifications.Semantic;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_translations;

public class with_reachable_translate_commands
{
    [Theory]
    [InlineData("direct", true)]
    [InlineData("reaction", true)]
    [InlineData("unrelated", false)]
    public async Task should_refuse_only_specifications_that_reach_the_translate_command(string action, bool reached)
    {
        var source = $$"""
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
                  command Complete
                    id String identifier
                    produces Followed
                      for id
                  specification Saving
                    when {{(action == "direct" ? "Complete" : "Save")}}
                      id = "one"
                    then {{(action == "direct" ? "Followed" : "Saved")}}
                      for "one"
                slice Automation Followups
                  reaction Follow
                    when {{(action == "unrelated" ? "Historical" : "Saved")}}
                      invokes Complete
                        id = "one"
                slice Translate LegacySync
                  readmodel Snapshot
                    id String key
            """;
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Work"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("work"), "work", "Work.play", source);
        var compilation = new SemanticModelCompiler().Compile("Work", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join(';', compilation.Diagnostics));
        var model = compilation.Value!.Model;
        var module = model.Application.Modules.Single();
        var feature = module.Features.Single();
        var command = feature.Slices.Single(slice => slice.Kind == SemanticSliceKind.StateChange).Commands.Single(command => command.Name == "Complete");
        var slices = feature.Slices.Select(slice => slice with
        {
            Commands = slice.Kind == SemanticSliceKind.Translate ? [command] : [.. slice.Commands.Where(candidate => candidate.Id != command.Id)]
        });
        var application = model.Application with { Modules = [module with { Features = [feature with { Slices = [.. slices] }] }] };
        var plan = SemanticExecutionPlan.Compile(ExecutableSemanticModel.Create(model.LanguageVersion, model.SemanticVersion, application)).Plan!;
        var result = (await new SemanticSpecificationExecutor().Run(plan, new([]), new())).Results.Single();

        result.Outcome.ShouldEqual(reached ? SemanticSpecificationOutcome.Unsupported : SemanticSpecificationOutcome.Passed);
        if (reached)
        {
            result.Unsupported!.Details.StartsWith("STAGE-ESM-024:", StringComparison.Ordinal).ShouldBeTrue();
            result.Unsupported.Construct.ShouldEqual(plan.Model.Application.Modules.Single().Features.Single().Slices.Single(slice => slice.Kind == SemanticSliceKind.Translate).Id.ToString());
        }
        else
        {
            result.Unsupported.ShouldBeNull();
        }
    }
}
