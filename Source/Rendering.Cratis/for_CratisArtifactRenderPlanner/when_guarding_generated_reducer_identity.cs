// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Reflection;
using Cratis.Chronicle.Events;
using Cratis.Stage.Contracts.Semantics;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_guarding_generated_reducer_identity
{
    const string First = "3fa85f64-5717-4562-b3fc-2c963f66afa6";
    const string Other = "4fa85f64-5717-4562-b3fc-2c963f66afa7";
    const string Body = "return new Total(Guid.Parse(\"00000000-0000-0000-0000-000000000001\"), context.Event.Amount);";

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task public_on_checks_guid_and_text_primitive_or_concept_keys(bool concept, bool text)
    {
        var source = when_rendering_a_pure_reducer.Source.Replace(Body, "if (context.Event.Amount < 0m) return null; if (context.Event.Amount == 2m && context.State is not null) return context.State; return new Total(context.Event.Id, context.Event.Amount);", StringComparison.Ordinal);
        if (concept)
        {
            source = when_rendering_a_pure_reducer.WithIdentifierConcept(source);
        }

        if (text)
        {
            source = source.Replace("concept OrderId : Uuid", "concept OrderId : String", StringComparison.Ordinal)
                .Replace("id Uuid identifier", "id String identifier", StringComparison.Ordinal)
                .Replace("id Uuid", "id String", StringComparison.Ordinal);
        }

        var plan = when_rendering_a_pure_reducer.Plan(await when_rendering_a_pure_reducer.Load(source));
        Assert.True(plan.Success, string.Join(Environment.NewLine, plan.Diagnostics));
        var files = plan.Artifacts.Where(artifact => artifact.RelativePath.EndsWith(".cs", StringComparison.Ordinal) && artifact.RelativePath != "Program.cs")
            .Select(artifact => new RenderedFile(artifact.RelativePath, System.Text.Encoding.UTF8.GetString(artifact.Bytes.AsSpan()))).ToArray();
        Assert.Empty(RenderedOutput.Errors(files));
        var assembly = RenderedOutput.Load(files);
        var record = assembly.GetTypes().Single(type => type.Name == "Total");
        var eventType = assembly.GetTypes().Single(type => type.Name == "OrderPlaced");
        var reducer = Activator.CreateInstance(assembly.GetTypes().Single(type => type.Name == "Fold"));
        var on = reducer!.GetType().GetMethods().Single(method => method.Name == "On");
        var identity = concept ? assembly.GetTypes().Single(type => type.Name == "OrderId") : null;
        object Key(string value)
        {
            object primitive = text ? value : Guid.Parse(value);
            return concept ? Activator.CreateInstance(identity!, primitive)! : primitive;
        }
        object? Invoke(object @event, object? current, string sourceId) => on.Invoke(reducer,
            [@event, current, EventContext.EmptyWithEventSourceId(new EventSourceId(sourceId)) with { SequenceNumber = new(0) }]);
        var created = Invoke(Activator.CreateInstance(eventType, Key(First), 20m)!, null, First);
        Assert.NotNull(created);
        var updated = Invoke(Activator.CreateInstance(eventType, Key(First), 25m)!, created, First);
        Assert.NotNull(updated);
        Assert.Equal(25m, record.GetProperty("Amount")!.GetValue(updated));
        Assert.Null(Invoke(Activator.CreateInstance(eventType, Key(First), -1m)!, updated, First));
        Assert.Null(Invoke(Activator.CreateInstance(eventType, Key(Other), -1m)!, updated, First));

        // The event payload is not the authority: Chronicle writes under the context's source id.
        var mismatch = Assert.Throws<TargetInvocationException>(() => Invoke(Activator.CreateInstance(eventType, Key(First), 1m)!, null, Other));
        Assert.IsType<InvalidOperationException>(mismatch.InnerException);
        if (!text)
        {
            // Parsing the UUID source would accept this. The original wire string must match exactly.
            var noncanonical = Assert.Throws<TargetInvocationException>(() => Invoke(Activator.CreateInstance(eventType, Key(First), 1m)!, null, First.ToUpperInvariant()));
            Assert.IsType<InvalidOperationException>(noncanonical.InnerException);
        }

        if (concept)
        {
            // A positional concept can be copied with a new Value while retaining its inherited TypedValue.
            // Both fields must be checked; neither one alone proves the persisted identity.
            var changedValue = Key(First);
            identity!.GetProperty("Value")!.SetValue(changedValue, text ? Other : Guid.Parse(Other));
            var divergent = Activator.CreateInstance(record, changedValue, 2m)!;
            var mismatchField = Assert.Throws<TargetInvocationException>(() => Invoke(Activator.CreateInstance(eventType, Key(First), 2m)!, divergent, First));
            Assert.IsType<InvalidOperationException>(mismatchField.InnerException);
            var changedTypedValue = Key(First);
            identity.BaseType!.GetProperty("TypedValue")!.SetValue(changedTypedValue, text ? Other : Guid.Parse(Other));
            var divergentTyped = Activator.CreateInstance(record, changedTypedValue, 2m)!;
            var mismatchTyped = Assert.Throws<TargetInvocationException>(() => Invoke(Activator.CreateInstance(eventType, Key(First), 2m)!, divergentTyped, First));
            Assert.IsType<InvalidOperationException>(mismatchTyped.InnerException);
        }
    }

    [Fact]
    public async Task every_public_on_overload_guards_its_own_transition()
    {
        var source = when_rendering_a_pure_reducer.Source.Replace(Body, "return new Total(context.Event.Id, context.Event.Amount);", StringComparison.Ordinal)
            .Replace("      event OrderPlaced\n        id Uuid\n        amount Decimal", "      event OrderPlaced\n        id Uuid\n        amount Decimal\n      event OrderAdjusted\n        id Uuid\n        amount Decimal", StringComparison.Ordinal)
            .Replace("          return new Total(context.Event.Id, context.Event.Amount);\n          ```", "          return new Total(context.Event.Id, context.Event.Amount);\n          ```\n        on OrderAdjusted\n          ```csharp\n          return new Total(context.Event.Id, context.Event.Amount);\n          ```", StringComparison.Ordinal);
        var plan = when_rendering_a_pure_reducer.Plan(await when_rendering_a_pure_reducer.Load(source));
        Assert.True(plan.Success, string.Join(Environment.NewLine, plan.Diagnostics));
        var files = plan.Artifacts.Where(artifact => artifact.RelativePath.EndsWith(".cs", StringComparison.Ordinal) && artifact.RelativePath != "Program.cs")
            .Select(artifact => new RenderedFile(artifact.RelativePath, System.Text.Encoding.UTF8.GetString(artifact.Bytes.AsSpan()))).ToArray();
        Assert.Empty(RenderedOutput.Errors(files));
        var assembly = RenderedOutput.Load(files);
        var reducer = Activator.CreateInstance(assembly.GetTypes().Single(type => type.Name == "Fold"));
        var overloads = reducer.GetType().GetMethods().Where(method => method.Name == "On").ToArray();
        Assert.Equal(2, overloads.Length);
        foreach (var on in overloads)
        {
            var @event = Activator.CreateInstance(on.GetParameters()[0].ParameterType, Guid.Parse(First), 1m);
            var context = EventContext.EmptyWithEventSourceId(new EventSourceId(Other)) with { SequenceNumber = new(0) };
            var exception = Assert.Throws<TargetInvocationException>(() => on.Invoke(reducer, [@event, null, context]));
            Assert.IsType<InvalidOperationException>(exception.InnerException);
        }
    }

    [Theory]
    [InlineData("Int")]
    [InlineData("Decimal")]
    [InlineData("Bool")]
    [InlineData("Date")]
    public async Task rejects_unsupported_primitive_identifier_shapes(string type)
    {
        var source = when_rendering_a_pure_reducer.Source.Replace(Body, "return context.State;", StringComparison.Ordinal)
            .Replace("      readmodel Total\n        id Uuid", $"      readmodel Total\n        id {type}", StringComparison.Ordinal)
            .Replace("      query ById => Total?\n        by id Uuid", $"      query ById => Total?\n        by id {type}", StringComparison.Ordinal);
        var plan = when_rendering_a_pure_reducer.Plan(await when_rendering_a_pure_reducer.Load(source));
        Assert.Contains(plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-019" && diagnostic.Message.Contains("identifier", StringComparison.Ordinal));
        Assert.Empty(plan.Artifacts);
    }

    [Theory]
    [InlineData("Int")]
    [InlineData("Decimal")]
    public async Task rejects_identifier_concepts_without_guid_or_text_wire_conversion(string type)
    {
        var source = when_rendering_a_pure_reducer.WithIdentifierConcept(when_rendering_a_pure_reducer.Source)
            .Replace("concept OrderId : Uuid", $"concept OrderId : {type}", StringComparison.Ordinal)
            .Replace(Body, "return context.State;", StringComparison.Ordinal);
        var plan = when_rendering_a_pure_reducer.Plan(await when_rendering_a_pure_reducer.Load(source));
        Assert.Contains(plan.Diagnostics, diagnostic => diagnostic.Code == "STAGE-ESM-019" && diagnostic.Message.Contains("identifier", StringComparison.Ordinal));
        Assert.Empty(plan.Artifacts);
    }

    [Fact]
    public async Task rejects_a_query_that_does_not_use_the_read_model_identifier_type()
    {
        var source = when_rendering_a_pure_reducer.Source.Replace(Body, "return context.State;", StringComparison.Ordinal)
            .Replace("      query ById => Total?\n        by id Uuid", "      query ById => Total?\n        by id String", StringComparison.Ordinal);
        var error = await Assert.ThrowsAsync<InvalidSemanticModel>(() => when_rendering_a_pure_reducer.Load(source));
        Assert.Contains("same scalar type", error.Message, StringComparison.Ordinal);
    }
}
#endif
