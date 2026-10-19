// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Cratis.Stage.Rendering.Cratis.Semantics.Projections;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

/// <summary>
/// The Screenplay compiler refuses most unrenderable composite shapes first (PLAY0660-PLAY0662, PLAY0268); the renderer admission
/// is the backstop for models other producers emit, so each reason is exercised by mutating a valid compiled model.
/// </summary>
public class when_refusing_composite_key_parts
{
    public static TheoryData<string, string> Reasons => new()
    {
        { nameof(Reducer), "only a scoped projection" },
        { nameof(Flat), "only a scoped projection" },
        { nameof(IdProperty), "collides with the generated key property" },
        { nameof(OptionalPart), "required, single, non-date" },
        { nameof(CollectionPart), "required, single, non-date" },
        { nameof(DateTimePart), "required, single, non-date" },
        { nameof(NonPrimitivePart), "required, single, non-date" },
        { nameof(Every), "only from and remove-with" },
        { nameof(Joins), "only from and remove-with" },
        { nameof(JoinRemovals), "only from and remove-with" },
        { nameof(Children), "only from and remove-with" },
        { nameof(Nested), "only from and remove-with" },
        { nameof(IncompleteKey), "needs a parts key" },
        { nameof(RemovalWithoutKey), "needs a parts key" },
        { nameof(MappingOntoKeyPart), "cannot also set a key part" }
    };

    [Fact]
    public void should_admit_the_unmodified_model()
    {
        var (context, model, projection, slice) = Parts();

        Assert.Null(SemanticCompositeKeyAdmission.Rejection(context, model, projection, slice));
    }

    [Theory]
    [MemberData(nameof(Reasons))]
    public void should_refuse_with_the_reason(string shape, string reason)
    {
        var (context, model, projection, slice) = Parts();
        (model, projection, slice) = shape switch
        {
            nameof(Reducer) => (model, null, slice),
            nameof(Flat) => (model, projection! with { Scope = null }, slice),
            nameof(IdProperty) => (model with { Properties = [.. model.Properties.Select(property => property.IsIdentifier ? property : property with { Name = "id" })] }, projection, slice),
            nameof(OptionalPart) => (WithKeyType(model, type => type with { IsOptional = true }), projection, slice),
            nameof(CollectionPart) => (WithKeyType(model, type => type with { IsCollection = true }), projection, slice),
            nameof(DateTimePart) => (WithKeyType(model, type => type with { Kind = SemanticTypeReferenceKind.Primitive, Primitive = SemanticPrimitiveType.DateTime }), projection, slice),
            nameof(NonPrimitivePart) => (WithKeyType(model, type => type with { Kind = SemanticTypeReferenceKind.CompositeType }), projection, slice),
            nameof(Every) => (model, WithScope(projection!, scope => scope with { Every = Placeholder<SemanticProjectionEvery>() }), slice),
            nameof(Joins) => (model, WithScope(projection!, scope => scope with { Joins = [Placeholder<SemanticProjectionJoin>()] }), slice),
            nameof(JoinRemovals) => (model, WithScope(projection!, scope => scope with { JoinRemovals = [Placeholder<SemanticProjectionJoinRemoval>()] }), slice),
            nameof(Children) => (model, WithScope(projection!, scope => scope with { Children = [Placeholder<SemanticProjectionChildren>()] }), slice),
            nameof(Nested) => (model, WithScope(projection!, scope => scope with { Nested = [Placeholder<SemanticProjectionNested>()] }), slice),
            nameof(IncompleteKey) => (model, WithScope(projection!, scope => scope with { From = [scope.From[0] with { Key = new SemanticProjectionPartsKey([.. ((SemanticProjectionPartsKey)scope.From[0].Key).Parts.Take(1)]) }] }), slice),
            nameof(RemovalWithoutKey) => (model, WithScope(projection!, scope => scope with { Removals = [scope.Removals[0] with { Key = new SemanticProjectionPartsKey([]) }] }), slice),
            nameof(MappingOntoKeyPart) => (model, WithScope(projection!, scope => scope with { From = [scope.From[0] with { Mappings = [new SemanticProjectionMapping([model.Properties.First(property => property.IsIdentifier).Id], SemanticProjectionOperation.Set, scope.From[0].Mappings[0].Source)] }] }), slice),
            _ => throw new InvalidOperationException(shape)
        };

        Assert.Contains(reason, SemanticCompositeKeyAdmission.Rejection(context, model, projection, slice));
    }

    static T Placeholder<T>() => (T)RuntimeHelpers.GetUninitializedObject(typeof(T));

    static SemanticReadModel WithKeyType(SemanticReadModel model, Func<SemanticTypeReference, SemanticTypeReference> change) =>
        model with { Properties = [.. model.Properties.Select((property, index) => property.IsIdentifier && index == model.Properties.ToList().FindIndex(_ => _.IsIdentifier) ? property with { Type = change(property.Type) } : property)] };

    static SemanticProjection WithScope(SemanticProjection projection, Func<SemanticProjectionScope, SemanticProjectionScope> change) =>
        projection with { Scope = change(projection.Scope!) };

    static (SemanticApplicationContext Context, SemanticReadModel Model, SemanticProjection? Projection, SemanticSlice Slice) Parts()
    {
        var model = invoice_model.Compile(when_refusing_composite_key_shapes.Valid);
        var options = new CratisRenderingOptions("InvoiceApp", "Invoices");
        var context = new SemanticApplicationContext(new(model, SemanticExecutionPlan.Compile(model).Plan!, CratisRendering.CreateProfile(model.Application.Name, options), new(ArtifactRenderScopeKind.Application, model.Application.Id)), options);
        var slice = context.SelectedSlices().Single(located => located.Slice.Name == "Totals").Slice;
        return (context, slice.ReadModels.Single(), slice.Projections.Single(), slice);
    }

    const string Reducer = nameof(Reducer);
    const string Flat = nameof(Flat);
    const string IdProperty = nameof(IdProperty);
    const string OptionalPart = nameof(OptionalPart);
    const string CollectionPart = nameof(CollectionPart);
    const string DateTimePart = nameof(DateTimePart);
    const string NonPrimitivePart = nameof(NonPrimitivePart);
    const string Every = nameof(Every);
    const string Joins = nameof(Joins);
    const string JoinRemovals = nameof(JoinRemovals);
    const string Children = nameof(Children);
    const string Nested = nameof(Nested);
    const string IncompleteKey = nameof(IncompleteKey);
    const string RemovalWithoutKey = nameof(RemovalWithoutKey);
    const string MappingOntoKeyPart = nameof(MappingOntoKeyPart);
}
