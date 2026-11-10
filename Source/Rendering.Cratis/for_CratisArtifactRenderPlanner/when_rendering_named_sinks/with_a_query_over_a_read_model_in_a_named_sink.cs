// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text.Json;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_rendering_named_sinks.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_rendering_named_sinks;

public class with_a_query_over_a_read_model_in_a_named_sink : Specification
{
    ArtifactRenderPlan _plan = null!;
    ArtifactRenderPlan _withoutSinks = null!;
    JsonDocument _settings = null!;

    static string WithQuery(string source) => source.Replace("      projection BuildListing => Listing\n", "      query AllListings => Listing[]\n      projection BuildListing => Listing\n", StringComparison.Ordinal);

    void Because()
    {
        _plan = a_model_with_named_sinks.Plan(a_model_with_named_sinks.Compile(a_model_with_named_sinks.Sinks + WithQuery(a_model_with_named_sinks.Source)));
        _withoutSinks = a_model_with_named_sinks.Plan(a_model_with_named_sinks.Compile(WithQuery(a_model_with_named_sinks.WithoutSinks)));
        _settings = JsonDocument.Parse(a_model_with_named_sinks.Text(_plan, "appsettings.json"));
    }

    JsonElement MongoDBSettings => _settings.RootElement.GetProperty("Cratis").GetProperty("MongoDB");

    [Fact] void should_render_successfully() => Assert.True(_plan.Success, string.Join("; ", _plan.Diagnostics));
    [Fact] void should_not_refuse_the_query() => _plan.Diagnostics.ShouldNotContain(diagnostic => diagnostic.Code == "STAGE-ESM-069" && diagnostic.Severity == ArtifactRenderDiagnosticSeverity.Error);
    [Fact] void should_render_the_query() => a_model_with_named_sinks.Text(_plan, "Directory.cs").ShouldContain("AllListings");
    [Fact] void should_store_the_read_model_in_its_sink() => a_model_with_named_sinks.Text(_plan, "Directory.cs").ShouldContain("[Sink(\"Reporting\")]");
    [Fact] void should_point_arc_at_the_development_mongodb_for_the_read_model_sink() => MongoDBSettings.GetProperty("Sinks").GetProperty("Reporting").GetProperty("Server").GetString().ShouldEqual("mongodb://localhost:27017");
    [Fact] void should_not_configure_the_graph_sink_for_arc_mongodb() => MongoDBSettings.GetProperty("Sinks").TryGetProperty("People", out _).ShouldBeFalse();
    [Fact] void should_keep_the_default_mongodb_connection() => MongoDBSettings.GetProperty("Server").GetString().ShouldEqual("mongodb://localhost:27017");
    [Fact] void should_keep_the_default_database() => MongoDBSettings.GetProperty("Database").GetString().ShouldEqual("Organization");
    [Fact] void should_render_the_query_without_sinks() => Assert.True(_withoutSinks.Success, string.Join("; ", _withoutSinks.Diagnostics));
    [Fact] void should_configure_no_arc_sink_without_a_declaration() => a_model_with_named_sinks.Text(_withoutSinks, "appsettings.json").ShouldNotContain("Sinks");
}
#endif
