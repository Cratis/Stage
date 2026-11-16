// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Lifecycles.for_LifecycleSubjects.when_building_a_command_source;

public class with_an_empty_snapshot : given.a_subject
{
    JsonObject _result = null!;

    void Because() => _result = LifecycleSubjects.CommandSource(_plan, _command, []);

    [Fact] void should_have_no_source_members() => _result.ShouldBeEmpty();
}
