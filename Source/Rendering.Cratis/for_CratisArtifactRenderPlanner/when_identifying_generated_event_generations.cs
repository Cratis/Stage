// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_identifying_generated_event_generations : Specification
{
    EventType _eventType = null!;

    void Because()
    {
        var model = invoice_model.Compile(invoice_model.Source("String", invoice_model.TextSource, invoice_model.OtherTextSource));
        var plan = invoice_model.Plan(model);
        Assert.True(plan.Success, string.Join(Environment.NewLine, plan.Diagnostics));
        var files = plan.Artifacts.Where(artifact => artifact.RelativePath.EndsWith(".cs", StringComparison.Ordinal) && artifact.RelativePath != "Program.cs")
            .Select(artifact => new RenderedFile(artifact.RelativePath, System.Text.Encoding.UTF8.GetString(artifact.Bytes.AsSpan())));
        var assembly = RenderedOutput.Load(files);
        _eventType = assembly.GetTypes().Single(type => type.Name == "InvoiceIssued").GetEventType();
    }

    [Fact] void should_preserve_the_existing_type_id_mapping() => _eventType.Id.Value.ShouldEqual("InvoiceIssued");
    [Fact] void should_identify_the_initial_event_generation() => _eventType.Generation.ShouldEqual(EventTypeGeneration.First);
}
