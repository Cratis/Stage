// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Reflection;
using Cratis.Chronicle;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_translating_reducer_tenants : given.a_generated_tenant_reducer
{
    protected override string Body => "return new Total(context.Event.Id, context.Tenant.Value.Length);";
    Dictionary<string, string> _values = null!;
    Dictionary<string, decimal> _observed = null!;
    List<Exception> _collisions = null!;
    List<Exception> _observedCollisions = null!;

    void Because()
    {
        if (!_plan.Success) return;
        var translate = _assembly.GetType("Projects.TypedContexts.ReducerContextValues")!.GetMethod("Tenant", BindingFlags.Public | BindingFlags.Static)!;
        var names = new[] { EventStoreNamespaceName.Default.Value, EventStoreNamespaceName.NotSet.Value, "North", "default", "10000000-0000-0000-0000-000000000000" };
        _values = names.ToDictionary(name => name, name => Value(translate.Invoke(null, [new EventStoreNamespaceName(name)])!));
        _observed = names.ToDictionary(name => name, Apply);
        var collisions = new[] { string.Empty, "00000000-0000-0000-0000-000000000000", "{00000000-0000-0000-0000-000000000000}", "00000000000000000000000000000000" };
        _collisions = [.. collisions.Select(name => Catch.Exception(() => translate.Invoke(null, [new EventStoreNamespaceName(name)])))];
        _observedCollisions = [.. collisions.Select(name => Catch.Exception(() => Apply(name)))];
    }

    [Fact] void should_admit_a_body_reading_the_tenant() => _plan.Success.ShouldBeTrue();
    [Fact] void should_translate_default_to_the_portable_zero_guid() => _values[EventStoreNamespaceName.Default.Value].ShouldEqual(Screenplay.Contexts.TenantId.Default.Value);
    [Fact] void should_translate_unset_to_the_portable_unset_value() => _values[EventStoreNamespaceName.NotSet.Value].ShouldEqual(Screenplay.Contexts.TenantId.NotSet.Value);
    [Fact] void should_preserve_named_tenants() => _values["North"].ShouldEqual("North");
    [Fact] void should_not_conflate_a_differently_cased_named_tenant_with_default() => _values["default"].ShouldEqual("default");
    [Fact] void should_preserve_a_nonzero_guid_named_tenant() => _values["10000000-0000-0000-0000-000000000000"].ShouldEqual("10000000-0000-0000-0000-000000000000");
    [Fact] void should_reject_names_colliding_with_portable_sentinels() => _collisions.TrueForAll(Ambiguous).ShouldBeTrue();
    [Fact] void should_supply_the_translated_tenant_through_the_generated_on_method() => _observed.All(pair => pair.Value == _values[pair.Key].Length).ShouldBeTrue();
    [Fact] void should_reject_ambiguous_names_before_invoking_a_tenant_reading_body() => _observedCollisions.TrueForAll(Ambiguous).ShouldBeTrue();

    static bool Ambiguous(Exception error) => error is TargetInvocationException { InnerException: { } inner } && inner.GetType().Name == "AmbiguousTenant";
    static string Value(object tenant) => (string)tenant.GetType().GetProperty("Value")!.GetValue(tenant)!;
}
#endif
