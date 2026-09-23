// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Scene.for_DefaultSceneComposition;

public class when_classifying_scalar_descriptors : a_register_project_render_request
{
    Exception? _unknown;
    SemanticPrimitiveType _collection;
    SemanticPrimitiveType _composite;

    void Because()
    {
        var context = new SemanticApplicationContext(_request, _options);
        _unknown = Catch.Exception(() => DefaultSceneComposition.Scalar(context,
            new((SemanticTypeReferenceKind)99, SemanticPrimitiveType.Unknown, default, false, false)));
        _collection = DefaultSceneComposition.Scalar(context, SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text, isCollection: true));
        _composite = DefaultSceneComposition.Scalar(context, SemanticTypeReference.ForCompositeType(default));
    }

    [Fact] void should_fail_on_unhandled_type_references() => _unknown.ShouldBeOfExactType<UnsupportedSemanticRendering>();
    [Fact] void should_keep_collections_non_scalar() => _collection.ShouldEqual(SemanticPrimitiveType.Unknown);
    [Fact] void should_keep_composites_non_scalar() => _composite.ShouldEqual(SemanticPrimitiveType.Unknown);
}
