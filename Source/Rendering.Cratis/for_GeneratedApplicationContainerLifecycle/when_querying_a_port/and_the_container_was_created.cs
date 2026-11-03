// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_GeneratedApplicationContainerLifecycle.given;
using Cratis.Stage.Rendering.Cratis.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_GeneratedApplicationContainerLifecycle.when_querying_a_port;

public class and_the_container_was_created : a_container
{
    async Task Establish()
    {
        ScriptCreate(Own.Value, Success());
        ScriptOwnedInspect(Own);
        _docker.On("port", Success("127.0.0.1:49153"));
        _docker.On("logs", Success("log line"));
        await _container.Create();
    }

    async Task Because()
    {
        await _container.Port("27017/tcp");
        await _container.Logs();
    }

    [Fact] void should_query_the_port_by_identifier() => Commands("port").Single().ShouldEqual($"port {Own} 27017/tcp");
    [Fact] void should_read_the_logs_by_identifier() => Commands("logs").Single().ShouldEqual($"logs {Own}");
}
