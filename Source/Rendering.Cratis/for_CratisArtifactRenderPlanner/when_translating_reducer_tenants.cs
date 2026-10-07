// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Reflection;
using Cratis.Chronicle;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_translating_reducer_tenants : Specification
{
    MethodInfo _translate;
    Dictionary<string, string> _values;
    List<Exception> _collisions;

    async Task Establish()
    {
        var source = when_rendering_a_pure_reducer.Source.Replace(
            "return new Total(Guid.Parse(\"00000000-0000-0000-0000-000000000001\"), context.Event.Amount);",
            "return new Total(context.Event.Id, context.Tenant.Value.Length);",
            StringComparison.Ordinal);
        var plan = when_rendering_a_pure_reducer.Plan(await when_rendering_a_pure_reducer.Load(source));
        Assert.True(plan.Success, string.Join(Environment.NewLine, plan.Diagnostics));
        var files = plan.Artifacts.Where(artifact => artifact.RelativePath.EndsWith(".cs", StringComparison.Ordinal) && artifact.RelativePath != "Program.cs")
            .Select(artifact => new RenderedFile(artifact.RelativePath, System.Text.Encoding.UTF8.GetString(artifact.Bytes.AsSpan())));
        var assembly = RenderedOutput.Load(files);
        _translate = assembly.GetType("Projects.TypedContexts.ReducerContextValues").GetMethod("Tenant", BindingFlags.Public | BindingFlags.Static);
    }

    void Because()
    {
        _values = new[] { EventStoreNamespaceName.Default.Value, EventStoreNamespaceName.NotSet.Value, "North", "default", "10000000-0000-0000-0000-000000000000" }
            .ToDictionary(name => name, name => Value(_translate.Invoke(null, [new EventStoreNamespaceName(name)])));
        _collisions = [.. new[] { string.Empty, "00000000-0000-0000-0000-000000000000", "{00000000-0000-0000-0000-000000000000}", "00000000000000000000000000000000" }
            .Select(name => Catch.Exception(() => _translate.Invoke(null, [new EventStoreNamespaceName(name)])))];
    }

    [Fact] void should_translate_default_to_the_portable_zero_guid() => _values[EventStoreNamespaceName.Default.Value].ShouldEqual(Screenplay.Contexts.TenantId.Default.Value);
    [Fact] void should_translate_unset_to_the_portable_unset_value() => _values[EventStoreNamespaceName.NotSet.Value].ShouldEqual(Screenplay.Contexts.TenantId.NotSet.Value);
    [Fact] void should_preserve_named_tenants() => _values["North"].ShouldEqual("North");
    [Fact] void should_not_conflate_a_differently_cased_named_tenant_with_default() => _values["default"].ShouldEqual("default");
    [Fact] void should_preserve_a_nonzero_guid_named_tenant() => _values["10000000-0000-0000-0000-000000000000"].ShouldEqual("10000000-0000-0000-0000-000000000000");
    [Fact] void should_reject_names_colliding_with_portable_sentinels() => _collisions.TrueForAll(error => error is TargetInvocationException { InnerException: InvalidOperationException }).ShouldBeTrue();

    static string Value(object tenant) => (string)tenant.GetType().GetProperty("Value").GetValue(tenant)!;
}
#endif
