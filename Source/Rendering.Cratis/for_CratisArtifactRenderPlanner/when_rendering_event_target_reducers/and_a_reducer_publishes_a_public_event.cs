// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Semantics;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_rendering_event_target_reducers;

public class and_a_reducer_publishes_a_public_event : Specification
{
    internal const string Declaration = """
        module Shop
          feature Sales
            slice StateChange Orders
              event Drafted
                name String
            slice Translate Publish
              direction outbound
              public event OrderPublished
                name String
        """;

    internal const string Reducer = """
              reducer OrderPublisher => OrderPublished
                on Drafted
                  ```csharp
                  if (context.Event.Name == "delete") return null;
                  return new OrderPublished(context.Event.Name);
                  ```
        """;

    LoadedSemanticModel _loaded = null!;
    ArtifactRenderPlan _plan = null!;
    string _reducer = string.Empty;

    async Task Establish() => _loaded = await when_rendering_a_pure_reducer.Load(Declaration + "\n" + Reducer);

    void Because()
    {
        _plan = when_rendering_a_pure_reducer.Plan(_loaded, self: true);
        Assert.True(_plan.Success, string.Join(Environment.NewLine, _plan.Diagnostics));
        _reducer = Encoding.UTF8.GetString(_plan.Artifacts.Single(artifact => artifact.RelativePath.EndsWith("OrderPublisher.cs", StringComparison.Ordinal)).Bytes.AsSpan());
    }

    [Fact] void should_render_a_reducer_for_the_contract_event() => Assert.Contains("IReducerFor<global::Projects.Shop.Sales.Publish.OrderPublished>", _reducer, StringComparison.Ordinal);
    [Fact] void should_use_instance_dispatch_even_for_a_self_reducing_profile() => Assert.Contains("? On(", _reducer, StringComparison.Ordinal);
    [Fact] void should_not_render_an_identity_check() => Assert.DoesNotContain("identifier different", _reducer, StringComparison.Ordinal);
    [Fact] void should_throw_rather_than_retract() => Assert.Contains("if (result is null) throw new PublicEventCannotBeRetracted();", _reducer, StringComparison.Ordinal);
    [Fact] void should_preserve_the_previous_instance_for_equal_values() => Assert.Contains("SamePublicValue(current, result) ? current : result", _reducer, StringComparison.Ordinal);
    [Fact] void should_compile() => Assert.Empty(RenderedOutput.Errors(_plan.Artifacts.Where(artifact => artifact.RelativePath.EndsWith(".cs", StringComparison.Ordinal) && artifact.RelativePath != "Program.cs")
        .Select(artifact => new RenderedFile(artifact.RelativePath, Encoding.UTF8.GetString(artifact.Bytes.AsSpan())))));

    [Fact]
    async Task should_not_change_the_contract_file()
    {
        var without = when_rendering_a_pure_reducer.Plan(await when_rendering_a_pure_reducer.Load(Declaration), self: true);
        Assert.True(without.Success, string.Join(Environment.NewLine, without.Diagnostics));
        var contract = _plan.Artifacts.Single(artifact => artifact.RelativePath.EndsWith("Publish.Events.cs", StringComparison.Ordinal));
        Assert.Equal<byte>(without.Artifacts.Single(artifact => artifact.RelativePath == contract.RelativePath).Bytes, contract.Bytes);
    }
}
#endif
