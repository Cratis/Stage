// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_rendering_named_sinks.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_rendering_named_sinks;

public class with_a_query_over_a_read_model_in_a_named_sink : Specification
{
    ArtifactRenderPlan _plan = null!;
    ArtifactRenderPlan _defaultSink = null!;

    static string WithQuery(string source) => source.Replace("      projection BuildListing => Listing\n", "      query AllListings => Listing[]\n      projection BuildListing => Listing\n", StringComparison.Ordinal);

    void Because()
    {
        _plan = a_model_with_named_sinks.Plan(a_model_with_named_sinks.Compile(a_model_with_named_sinks.Sinks + WithQuery(a_model_with_named_sinks.Source)));
        _defaultSink = a_model_with_named_sinks.Plan(a_model_with_named_sinks.Compile(a_model_with_named_sinks.Sinks + WithQuery(a_model_with_named_sinks.Source.Replace("        sink Reporting\n", string.Empty, StringComparison.Ordinal))));
    }

    ArtifactRenderDiagnostic Refusal => _plan.Diagnostics.Single(diagnostic => diagnostic.Code == "STAGE-ESM-069");

    [Fact] void should_refuse_the_render() => _plan.Success.ShouldBeFalse();
    [Fact] void should_render_nothing() => _plan.Artifacts.ShouldBeEmpty();
    [Fact] void should_refuse_it_as_an_error() => Refusal.Severity.ShouldEqual(ArtifactRenderDiagnosticSeverity.Error);
    [Fact] void should_name_the_read_model() => Refusal.Message.ShouldContain("read model 'Listing'");
    [Fact] void should_name_the_sink() => Refusal.Message.ShouldContain("sink 'Reporting'");
    [Fact] void should_render_the_query_over_a_read_model_in_the_default_sink() => Assert.True(_defaultSink.Success, string.Join("; ", _defaultSink.Diagnostics));
}
#endif
