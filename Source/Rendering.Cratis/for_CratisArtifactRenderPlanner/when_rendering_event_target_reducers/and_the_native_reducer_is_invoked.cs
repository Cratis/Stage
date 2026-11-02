// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Reflection;
using System.Text;
using Cratis.Chronicle.Events;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_rendering_event_target_reducers;

public class and_the_native_reducer_is_invoked : Specification
{
    object _reducer = null!;
    MethodInfo _on = null!;
    Type _event = null!;
    object? _initial;
    object? _unchanged;
    object? _changed;
    Exception? _failure;

    async Task Establish()
    {
        var loaded = await when_rendering_a_pure_reducer.Load(and_a_reducer_publishes_a_public_event.Declaration + "\n" + and_a_reducer_publishes_a_public_event.Reducer);
        var plan = when_rendering_a_pure_reducer.Plan(loaded, self: true);
        Assert.True(plan.Success, string.Join(Environment.NewLine, plan.Diagnostics));
        var assembly = RenderedOutput.Load(plan.Artifacts.Where(artifact => artifact.RelativePath.EndsWith(".cs", StringComparison.Ordinal) && artifact.RelativePath != "Program.cs")
            .Select(artifact => new RenderedFile(artifact.RelativePath, Encoding.UTF8.GetString(artifact.Bytes.AsSpan()))));
        var type = assembly.GetTypes().Single(type => type.Name == "OrderPublisher");
        _reducer = Activator.CreateInstance(type)!;
        _on = type.GetMethod("On")!;
        _event = assembly.GetTypes().Single(type => type.Name == "Drafted");
    }

    void Because()
    {
        _initial = Fold("first", null);
        _unchanged = Fold("first", _initial);
        _changed = Fold("second", _initial);
        _failure = Catch.Exception(() => Fold("delete", _initial));
    }

    [Fact] void should_create_the_first_public_value() => Assert.Equal("first", _initial!.GetType().GetProperty("Name")!.GetValue(_initial));
    [Fact] void should_suppress_duplicate_publication_by_preserving_the_previous_instance() => Assert.Same(_initial, _unchanged);
    [Fact] void should_return_the_changed_public_value() => Assert.Equal("second", _changed!.GetType().GetProperty("Name")!.GetValue(_changed));
    [Fact] void should_fail_loudly_instead_of_retracting() => Assert.Equal("PublicEventCannotBeRetracted", Assert.IsType<TargetInvocationException>(_failure).InnerException!.GetType().Name);

    object? Fold(string name, object? current) => _on.Invoke(_reducer, [Activator.CreateInstance(_event, name), current, EventContext.EmptyWithEventSourceId("source") with { SequenceNumber = new(1) }]);
}
#endif
