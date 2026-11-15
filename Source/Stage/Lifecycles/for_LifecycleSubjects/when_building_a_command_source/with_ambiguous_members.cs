// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Xunit;

namespace Cratis.Stage.Lifecycles.for_LifecycleSubjects.when_building_a_command_source;

public class with_ambiguous_members : given.a_subject
{
    void Establish() => Models(_subject with { Properties = [.. _subject.Properties, _status with { Id = Id(105), Name = "STATUS" }] });

    [Fact]
    void should_reject_case_variant_members_even_when_only_one_has_a_value() =>
        Assert.Throws<InvalidOperationException>(() => LifecycleSubjects.CommandSource(_plan, _command, Status(SemanticValue.Text("sent"))));
}
