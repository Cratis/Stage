// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Xunit;

namespace Cratis.Stage.Lifecycles.for_LifecycleSubjects.when_building_a_command_source;

public class with_wrong_types : given.a_subject
{
    [Theory]
    [InlineData("primitive")]
    [InlineData("optional")]
    [InlineData("collection")]
    [InlineData("concept")]
    [InlineData("target")]
    public void should_reject_nonidentical_semantic_types(string difference)
    {
        var member = difference == "target" ? _subject.Properties.Single(property => property.Name == "lines") : _status;
        var type = difference switch
        {
            "primitive" => SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Boolean),
            "optional" => member.Type with { IsOptional = true },
            "collection" => member.Type with { IsCollection = true },
            "target" => member.Type with { Target = Id(110) },
            _ => _subject.Properties.Single(property => property.IsIdentifier).Type
        };
        var command = _command with { Properties = [member with { Type = type }] };
        Assert.Throws<InvalidOperationException>(() => LifecycleSubjects.CommandSource(_plan, command, [new(member.Id, SemanticValue.Null)]));
    }
}
