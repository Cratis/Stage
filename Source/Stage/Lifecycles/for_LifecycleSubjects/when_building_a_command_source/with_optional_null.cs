// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Lifecycles.for_LifecycleSubjects.when_building_a_command_source;

public class with_optional_null : given.a_subject
{
    void Establish()
    {
        _status = _status with { Type = _status.Type with { IsOptional = true } };
        Models(_subject with { Properties = [.. _subject.Properties.Select(property => property.Id == _status.Id ? _status : property)] });
        _command = _command with { Properties = [_status] };
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void should_keep_null_binding_semantics_for_supplied_or_absent_values(bool supplied)
    {
        var identifier = _subject.Properties.Single(property => property.IsIdentifier);
        var source = LifecycleSubjects.CommandSource(_plan, _command, supplied ? Status(SemanticValue.Null) : [new(identifier.Id, SemanticValue.Text("invoice"))]);
        source.ContainsKey("status").ShouldBeTrue();
        source["status"].ShouldBeNull();
        LifecycleSubjects.CommandPayload(_plan, _command, "invoice", source).ShouldBeEmpty();
    }
}
