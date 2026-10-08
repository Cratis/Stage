// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_a_reducer_does_not_read_the_tenant : given.a_generated_tenant_reducer
{
    protected override string Body => "return new Total(context.Event.Id, context.Event.Amount);";
    decimal _empty;
    decimal _zero;

    void Because()
    {
        if (!_plan.Success) return;
        _empty = Apply(string.Empty);
        _zero = Apply("00000000-0000-0000-0000-000000000000");
    }

    [Fact] void should_admit_the_reducer() => _plan.Success.ShouldBeTrue();
    [Fact] void should_not_translate_an_empty_namespace() => _empty.ShouldEqual(7m);
    [Fact] void should_not_translate_a_zero_guid_namespace() => _zero.ShouldEqual(7m);
}
#endif
