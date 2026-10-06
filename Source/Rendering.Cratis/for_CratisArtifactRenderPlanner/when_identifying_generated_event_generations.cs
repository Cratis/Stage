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
    readonly List<(uint Revision, EventType Type)> _eventTypes = [];

    void Because()
    {
        foreach (var generation in new uint[] { 1, 2, 3 })
        {
            var source = invoice_model.Source("String", invoice_model.TextSource, invoice_model.OtherTextSource);
            if (generation > 1)
            {
                var declarations = string.Join('\n', Enumerable.Range(1, (int)generation).Select(revision =>
                    $"      event InvoiceIssued generation {revision}\n        description String"));
                source = source.Replace("      event InvoiceIssued\n        description String", declarations, StringComparison.Ordinal);
            }

            var model = invoice_model.Compile(source);
            var plan = invoice_model.Plan(model);
            Assert.True(plan.Success, string.Join(Environment.NewLine, plan.Diagnostics));
            var files = plan.Artifacts.Where(artifact => artifact.RelativePath.EndsWith(".cs", StringComparison.Ordinal) && artifact.RelativePath != "Program.cs")
                .Select(artifact => new RenderedFile(artifact.RelativePath, System.Text.Encoding.UTF8.GetString(artifact.Bytes.AsSpan())));
            var assembly = RenderedOutput.Load(files);
            var eventType = assembly.GetTypes().Single(type => type.Name == "InvoiceIssued").GetEventType();
            var modeledEvent = model.Application.Modules.Single().Features.Single().Slices.Single().Events.Single();
            _eventTypes.Add((modeledEvent.Revision.Value, eventType));
        }
    }

    [Fact] void should_compile_an_initial_and_two_evolved_event_shapes() => _eventTypes.Select(result => result.Revision).ShouldEqual(1u, 2u, 3u);
    [Fact] void should_preserve_the_existing_type_id_mapping() => _eventTypes.TrueForAll(result => result.Type.Id.Value == "InvoiceIssued").ShouldBeTrue();
    [Fact] void should_identify_each_event_as_its_modeled_generation() => _eventTypes.TrueForAll(result => result.Type.Generation.Value == result.Revision).ShouldBeTrue();
}
