// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Execution;
using Cratis.Stage.Contracts.Semantics;
using Cratis.Stage.Rendering.Cratis.Execution.for_BodyWorkerSupervisor.given;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Execution.for_BodyWorkerSupervisor;

public class when_evaluating_a_rendered_predicate : Specification
{
    [Fact]
    void should_remove_its_temporary_units_when_disposed()
    {
        var fixture = new a_real_worker();
        var path = fixture.CacheDirectory;
        fixture.Dispose();
        Directory.Exists(path).ShouldBeFalse();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    async Task should_compile_in_the_worker_and_return_true_and_false_without_host_loading(bool escapedProperty)
    {
        using var fixture = new a_real_worker();
        var path = Path.Combine(fixture.CacheDirectory, "Model.play");
        var property = escapedProperty ? "placeOrder" : "amount";
        var wire = escapedProperty ? "placeOrder_" : "amount";
        var source = validation_body_model.Command($"return context.Property == \"{wire}\" && context.Value > 0;").Replace("amount", property, StringComparison.Ordinal);
        await File.WriteAllTextAsync(path, source);
        await using var supervisor = fixture.Create(3000);
        var compiled = await supervisor.SendAsync(new("compile", "compile") { ModelPath = path, CacheDirectory = fixture.CacheDirectory });
        var unit = Assert.Single(compiled.Manifest!.Entries);
        Assert.True(unit.Admitted, unit.DiagnosticMessage);
        var loaded = await SemanticModelLoader.LoadFromPathAsync(path);
        unit.PropertyPaths[property].ShouldEqual(wire);
        var context = BodyUnitCompiler.CreateContext(loaded);
        var rendered = SemanticStateChangeArtifactRenderer.Render(context.AllSlices.Single(), context).Content;
        Assert.Contains($"\"{wire}\"", rendered.Replace($"context.Property == \"{wire}\"", "true", StringComparison.Ordinal), StringComparison.Ordinal);
        var command = loaded.Model.Application.Modules.Single().Features.Single().Slices.Single().Commands.Single();
        foreach (var amount in new[] { 1, -1 })
        {
            var properties = command.Properties.Select(property => new SemanticPropertyValue(property.Id, property.IsIdentifier
                ? SemanticValue.Text("3fa85f64-5717-4562-b3fc-2c963f66afa6") : SemanticValue.Number(amount))).ToImmutableArray();
            var members = new Dictionary<string, System.Text.Json.JsonElement>
            {
                ["Value"] = SemanticValueWire.Write(SemanticValue.Number(amount)),
                ["Artifact"] = SemanticValueWire.Write(SemanticValue.Composite(properties)),
                ["EventSourceId"] = SemanticValueWire.Write(SemanticValue.Text("3fa85f64-5717-4562-b3fc-2c963f66afa6")),
                ["Property"] = SemanticValueWire.Write(SemanticValue.Text(unit.PropertyPaths[property]))
            }.ToImmutableDictionary();
            var response = await supervisor.SendAsync(new(amount.ToString(System.Globalization.CultureInfo.InvariantCulture), "evaluate")
            {
                Unit = unit,
                CacheDirectory = fixture.CacheDirectory,
                Evaluation = new(unit.RequirementId, members)
            });
            Assert.True(response.Evaluation!.Outcome == (amount > 0 ? "satisfied" : "unsatisfied"), response.Evaluation.Diagnostic);
            SemanticValueWire.Read(response.Evaluation.Value!.Value).ShouldEqual(SemanticValue.Boolean(amount > 0));
        }
        AppDomain.CurrentDomain.GetAssemblies().Any(assembly => assembly.GetName().Name!.StartsWith("BodyUnit_", StringComparison.Ordinal)).ShouldBeFalse();
    }
}
