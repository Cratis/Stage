// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticCratisAdmission.when_admitting_whole_number_literals;

public class with_a_pre_v8_model
{
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    public void should_refuse_out_of_range_literals_without_emitting_artifacts(int major)
    {
        var plan = Plan(major, "2147483648", "-2147483649");
        plan.Success.ShouldBeFalse();
        plan.Artifacts.ShouldBeEmpty();
        plan.Diagnostics.Select(diagnostic => diagnostic.Code).Distinct().ShouldContainOnly("STAGE-ESM-031");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    public void should_admit_the_int_boundaries(int major) => Plan(major, "2147483647", "-2147483648").Success.ShouldBeTrue();

    static ArtifactRenderPlan Plan(int major, string positive, string negative)
    {
        var source = invoice_model.Source("Int", positive, negative);
        var marker = major switch
        {
            3 => """
                slice StateChange VersionMarker
                  command MarkVersion
                    id String identifier
                    validate csharp
                      ```csharp
                      return true;
                      ```
                    produces VersionMarked
                      for id
                  event VersionMarked
            """,
            4 => """
                slice StateChange VersionMarker
                  event VersionMarked generation 1
                    marker String
                  event VersionMarked generation 2
                    marker String
            """,
            5 => """
                slice StateView VersionMarker
                  event VersionRemoved
                    id String
                  readmodel VersionView
                    id String
                  query ById => VersionView optional
                    by id String
                  projection Versions => VersionView
                    remove with VersionRemoved key id
                  specification RemovingVersion
                    given readmodel VersionView
                      id = "first"
                    when append VersionRemoved
                      id = "first"
                    then no readmodel VersionView for "first"
            """,
            6 => """
                slice Automation VersionMarker
                  reaction MarkerScheduler
                    when InvoiceIssued
                      payload
                      produces VersionMarked
                        markedAt = $context.occurred
                  event VersionMarked
                    markedAt DateTime
            """,
            _ => string.Empty
        };
        if (major == 7) source = "policy VersionMarker\n  require not authenticated\n" + source;
        if (major == 6) source = source.Replace("description", "payload", StringComparison.Ordinal);
        var model = invoice_model.Compile(source + "\n" + marker);
        if (major == 1)
        {
            var module = model.Application.Modules[0];
            var feature = module.Features[0];
            var slice = feature.Slices[0];
            slice = slice with { Commands = [.. slice.Commands.Select(command => command with { Destination = null })] };
            model = ExecutableSemanticModel.Create(LanguageVersion.V1, SemanticVersion.V1, model.Application with
            {
                Modules = [module with { Features = [feature with { Slices = [slice] }] }]
            });
        }
        Convert.ToInt32(model.SemanticVersion.Major).ShouldEqual(major);
        var selected = model.Application.Modules[0].Features[0].Slices[0];
        if (major == 3)
        {
            var loaded = when_rendering_a_pure_reducer.Load(source + "\n" + marker).GetAwaiter().GetResult();
            var loadedSlice = loaded.Model.Application.Modules[0].Features[0].Slices[0];
            return when_rendering_a_pure_reducer.Plan(loaded, new(ArtifactRenderScopeKind.Slice, loadedSlice.Id));
        }

        return invoice_model.Plan(model, new(ArtifactRenderScopeKind.Slice, selected.Id));
    }
}
