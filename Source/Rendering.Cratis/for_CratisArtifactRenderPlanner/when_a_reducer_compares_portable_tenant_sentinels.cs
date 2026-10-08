// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Chronicle;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_a_reducer_compares_portable_tenant_sentinels : given.a_generated_tenant_reducer
{
    protected override string Body => "return new Total(context.Event.Id, context.Tenant == TenantId.Default ? 1m : context.Tenant == TenantId.NotSet ? 2m : 3m);";
    decimal _default;
    decimal _notSet;
    decimal _named;

    void Because()
    {
        if (!_plan.Success) return;
        _default = Apply(EventStoreNamespaceName.Default.Value);
        _notSet = Apply(EventStoreNamespaceName.NotSet.Value);
        _named = Apply("North");
    }

    [Fact] void should_admit_comparisons_with_the_portable_sentinels() => _plan.Success.ShouldBeTrue();
    [Fact] void should_recognize_default_in_the_generated_on_method() => _default.ShouldEqual(1m);
    [Fact] void should_recognize_not_set_in_the_generated_on_method() => _notSet.ShouldEqual(2m);
    [Fact] void should_not_conflate_a_named_tenant_with_either_sentinel() => _named.ShouldEqual(3m);
}
#endif
