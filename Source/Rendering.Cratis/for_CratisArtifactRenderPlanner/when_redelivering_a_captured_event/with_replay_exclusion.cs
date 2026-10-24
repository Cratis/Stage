// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Reflection;
using Cratis.Chronicle.Reactors;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_redelivering_a_captured_event;

public class with_replay_exclusion : given.a_captured_delivery
{
    void Because() => Render();

    [Fact] void should_keep_capture_replay_excluded() => Reactor().GetType().GetCustomAttribute<OnceOnlyAttribute>().ShouldNotBeNull();
}
#endif
