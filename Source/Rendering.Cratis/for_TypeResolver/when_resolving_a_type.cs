// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_TypeResolver.given;
using Cratis.Stage.Rendering.Cratis.Types;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_TypeResolver;

public class when_resolving_a_type : an_application_set
{
    ResolvedType _primitive = null!;
    ResolvedType _collection = null!;
    ResolvedType _optional = null!;
    ResolvedType _concept = null!;
    ResolvedType _enumConcept = null!;
    ResolvedType _composite = null!;
    ResolvedType _unresolved = null!;

    void Because()
    {
        _primitive = TypeResolver.Resolve(new TypeRefSyntax("String", false, false, SourceLocation.Start), _applicationSet);
        _collection = TypeResolver.Resolve(new TypeRefSyntax("String", true, false, SourceLocation.Start), _applicationSet);
        _optional = TypeResolver.Resolve(new TypeRefSyntax("Int", false, true, SourceLocation.Start), _applicationSet);
        _concept = TypeResolver.Resolve(new TypeRefSyntax("Money", false, false, SourceLocation.Start), _applicationSet);
        _enumConcept = TypeResolver.Resolve(new TypeRefSyntax("Status", false, false, SourceLocation.Start), _applicationSet);
        _composite = TypeResolver.Resolve(new TypeRefSyntax("Address", false, false, SourceLocation.Start), _applicationSet);
        _unresolved = TypeResolver.Resolve(new TypeRefSyntax("SomethingUnknown", false, false, SourceLocation.Start), _applicationSet);
    }

    [Fact] void should_resolve_a_primitive_to_its_clr_type() => _primitive.ClrTypeName.ShouldEqual("string");
    [Fact] void should_mark_a_primitive_as_primitive_kind() => _primitive.Kind.ShouldEqual(ResolvedTypeKind.Primitive);
    [Fact] void should_render_a_collection_as_ireadonlylist() => _collection.ToTypeSyntax().ShouldEqual("IReadOnlyList<string>");
    [Fact] void should_render_an_optional_type_with_a_nullable_suffix() => _optional.ToTypeSyntax().ShouldEqual("int?");
    [Fact] void should_resolve_a_concept_to_its_pascal_case_name() => _concept.ClrTypeName.ShouldEqual("Money");
    [Fact] void should_mark_a_plain_concept_as_concept_kind() => _concept.Kind.ShouldEqual(ResolvedTypeKind.Concept);
    [Fact] void should_mark_an_enum_concept_as_enum_kind() => _enumConcept.Kind.ShouldEqual(ResolvedTypeKind.Enum);
    [Fact] void should_resolve_a_composite_type_to_its_pascal_case_name() => _composite.ClrTypeName.ShouldEqual("Address");
    [Fact] void should_mark_a_composite_type_as_composite_kind() => _composite.Kind.ShouldEqual(ResolvedTypeKind.Composite);
    [Fact] void should_fall_back_to_object_for_an_unresolved_reference() => _unresolved.ClrTypeName.ShouldEqual("object");
    [Fact] void should_mark_an_unresolved_reference_as_unresolved_kind() => _unresolved.Kind.ShouldEqual(ResolvedTypeKind.Unresolved);
}
