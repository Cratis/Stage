// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Reflection;
using Cratis.Chronicle;
using Cratis.Chronicle.Events;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;

public abstract class a_generated_tenant_reducer : Specification
{
    protected ArtifactRenderPlan _plan = null!;
    protected Assembly _assembly = null!;
    protected abstract string Body { get; }
    readonly Guid _key = Guid.Parse("10000000-0000-0000-0000-000000000001");
    object _event = null!;
    object _reducer = null!;
    MethodInfo _on = null!;

    async Task Establish()
    {
        var source = when_rendering_a_pure_reducer.Source.Replace(
            "return new Total(Guid.Parse(\"00000000-0000-0000-0000-000000000001\"), context.Event.Amount);",
            Body,
            StringComparison.Ordinal);
        _plan = when_rendering_a_pure_reducer.Plan(await when_rendering_a_pure_reducer.Load(source));
        if (!_plan.Success) return;
        var files = _plan.Artifacts.Where(artifact => artifact.RelativePath.EndsWith(".cs", StringComparison.Ordinal) && artifact.RelativePath != "Program.cs")
            .Select(artifact => new RenderedFile(artifact.RelativePath, System.Text.Encoding.UTF8.GetString(artifact.Bytes.AsSpan())));
        _assembly = RenderedOutput.Load(files);
        _event = Activator.CreateInstance(_assembly.GetTypes().Single(type => type.Name == "OrderPlaced"), _key, 7m)!;
        var reducer = _assembly.GetTypes().Single(type => type.Name == "Fold");
        _reducer = Activator.CreateInstance(reducer)!;
        _on = reducer.GetMethod("On")!;
    }

    protected decimal Apply(string tenant)
    {
        var context = EventContext.Empty with { EventSourceId = _key.ToString(), Namespace = new EventStoreNamespaceName(tenant), SequenceNumber = 0UL };
        var result = _on.Invoke(_reducer, [_event, null, context])!;

        return (decimal)result.GetType().GetProperty("Amount")!.GetValue(result)!;
    }
}
#endif
