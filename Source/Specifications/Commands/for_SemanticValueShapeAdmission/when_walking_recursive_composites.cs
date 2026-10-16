// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Specifications.Commands.for_SemanticValueShapeAdmission;

public class when_walking_recursive_composites : given.a_value_shape
{
    (string Path, SemanticTypeReference Type)[] _closure = [];
    string? _refusal;

    void Because()
    {
        _closure = [.. SemanticValueShapeAdmission.Closure(_context, _type)];
        _refusal = SemanticValueShapeAdmission.Refusal(_context, _type);
    }

    [Fact] void should_admit_optional_recursive_composites_and_collections() => _refusal.ShouldBeNull();
    [Fact] void should_walk_in_declaration_order_and_stop_on_recursion() => _closure.Select(member => member.Path).ShouldEqual([string.Empty, "count", "next", "children"]);
}
