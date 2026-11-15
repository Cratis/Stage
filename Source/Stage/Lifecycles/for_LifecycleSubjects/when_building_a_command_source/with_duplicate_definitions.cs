// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Xunit;

namespace Cratis.Stage.Lifecycles.for_LifecycleSubjects.when_building_a_command_source;

public class with_duplicate_definitions : given.a_subject
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void should_reject_duplicate_property_definitions_in_one_or_several_models(bool sameOwner)
    {
        if (sameOwner)
        {
            Models(_subject with { Properties = [.. _subject.Properties, _status] });
        }
        else
        {
            Models(_subject, _subject with { Id = Id(107), Name = "Other", Properties = [_status] });
        }

        Assert.Throws<InvalidOperationException>(() => LifecycleSubjects.CommandSource(_plan, _command, Status(SemanticValue.Text("sent"))));
    }
}
