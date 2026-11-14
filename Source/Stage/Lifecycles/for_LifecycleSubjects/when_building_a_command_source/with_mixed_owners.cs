// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Xunit;

namespace Cratis.Stage.Lifecycles.for_LifecycleSubjects.when_building_a_command_source;

public class with_mixed_owners : given.a_subject
{
    [Fact]
    void should_reject_values_from_different_read_models()
    {
        var other = _status with { Id = Id(108) };
        Models(_subject, _subject with { Id = Id(109), Name = "Other", Properties = [other] });
        Assert.Throws<InvalidOperationException>(() => LifecycleSubjects.CommandSource(_plan, _command,
            [new(_status.Id, SemanticValue.Text("sent")), new(other.Id, SemanticValue.Text("other"))]));
    }
}
