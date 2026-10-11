// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Semantics;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticEventSourceArtifactRenderer;

public class when_placing_event_sources : Specification
{
    CratisPlanResult _plan = null!;
    CratisPlanResult _scaffold = null!;

    void Because()
    {
        const string declarations = """
            concept Year : Int
            concept CustomerId : Uuid
            eventsource Customer
              identifier CustomerId
              stream Notes
            module Banking
            """;
        var model = invoice_model.Compile(when_planning_event_source_routes.Source.Replace("module Banking", declarations, StringComparison.Ordinal).Replace("stream Transactions", "stream Statements\n    streamId Year\n  stream Transactions", StringComparison.Ordinal));
        var options = new CratisPlanOptions("InvoiceModel", "Banking", "Banking") { Domain = "Sales/Retail" };
        _scaffold = CratisRendering.PlanScaffold(options with { Domain = string.Empty });
        _plan = CratisRendering.PlanFrom(new LoadedSemanticModel(model, SemanticExecutionPlan.Compile(model).Plan!), new([PlanSelectionEntry.Slice("Banking", "Deposits", "Deposit")]), options);
    }

    [Fact] void should_admit_the_source_based_plan() => _plan.Success.ShouldBeTrue();
    [Fact] void should_place_definitions_under_the_domain() => _plan.Artifacts.Any(artifact => artifact.RelativePath == "Sales/Retail/EventSources/AccountEventSource.cs").ShouldBeTrue();
    [Fact] void should_include_unused_stream_type_dependencies_of_the_selected_definition() => _plan.Artifacts.Any(artifact => artifact.RelativePath == "Sales/Retail/Common/Year.cs").ShouldBeTrue();
    [Fact] void should_exclude_unreferenced_definitions() => _plan.Artifacts.Any(artifact => artifact.RelativePath.EndsWith("CustomerEventSource.cs", StringComparison.Ordinal)).ShouldBeFalse();
    [Fact] void should_exclude_unreferenced_source_identifiers() => _plan.Artifacts.Any(artifact => artifact.RelativePath.EndsWith("CustomerId.cs", StringComparison.Ordinal)).ShouldBeFalse();
    [Fact] void should_keep_the_shared_codec_at_the_root() => _plan.Artifacts.Any(artifact => artifact.RelativePath == "GeneratedEventSources/StreamIds.cs").ShouldBeTrue();
    [Fact] void should_compile_with_the_root_scaffold() => RenderedOutput.Errors(_plan.Artifacts.Concat(_scaffold.Artifacts).Where(artifact => artifact.RelativePath.EndsWith(".cs", StringComparison.Ordinal) && artifact.RelativePath != "Program.cs").Select(artifact => new RenderedFile(artifact.RelativePath, Encoding.UTF8.GetString(artifact.Bytes.AsSpan())))).ShouldBeEmpty();
}
