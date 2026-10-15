// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticValueShapeAdmission;

public class when_refusing_nested_unknown_types : given.a_value_shape
{
    string? _refusal;

    void Establish()
    {
        var types = (Dictionary<SemanticId, SemanticCompositeType>)_context.Types;
        var type = types[_type.Target];
        types[type.Id] = type with { Properties = [type.Properties[0] with { Type = SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Unknown) }] };
    }
    void Because() => _refusal = SemanticValueShapeAdmission.Refusal(_context, _type);

    [Fact] void should_refuse_an_unknown_type_in_the_closure() => _refusal.ShouldEqual("The value has an unresolved type.");
}
