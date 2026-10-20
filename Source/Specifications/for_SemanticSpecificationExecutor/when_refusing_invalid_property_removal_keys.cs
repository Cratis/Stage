// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor;

public class when_refusing_invalid_property_removal_keys : Specification
{
    [Theory]
    [InlineData("optional")]
    [InlineData("collection")]
    [InlineData("missing")]
    [InlineData("wrong-type")]
    [InlineData("parent")]
    [InlineData("composite")]
    public void should_reject_an_invalid_projection_contract_before_execution(string shape)
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Billing"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("model"), "model", "Model.play", when_running_property_keyed_removal_only_projections.Source);
        var compilation = new SemanticModelCompiler().Compile("Billing", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join(';', compilation.Diagnostics));
        var model = compilation.Value!.Model;
        var module = model.Application.Modules.Single();
        var feature = module.Features.Single();
        var slice = feature.Slices.Single(slice => !slice.Projections.IsEmpty);
        var @event = slice.Events.Single();
        var property = @event.Properties.Single();
        var projection = slice.Projections.Single();
        var removal = projection.Scope!.Removals.Single();
        var type = shape switch
        {
            "optional" => property.Type with { IsOptional = true },
            "collection" => property.Type with { IsCollection = true },
            "wrong-type" => SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.WholeNumber),
            _ => property.Type
        };
        if (shape == "missing") removal = removal with { Key = new SemanticProjectionValueKey(new SemanticProjectionEventProperty([SemanticId.Parse($"sem1:{1000:x64}")])) };
        if (shape == "parent") removal = removal with { ParentKey = removal.Key };
        if (shape == "composite") removal = removal with { Key = new SemanticProjectionPartsKey([new(property.Id, new SemanticProjectionEventProperty([property.Id]))]) };
        slice = slice with
        {
            Events = [@event with { Properties = [property with { Type = type }] }],
            Projections = [projection with { Scope = projection.Scope with { Removals = [removal] } }]
        };
        Assert.Throws<InvalidSemanticContract>(() => ExecutableSemanticModel.Create(model.LanguageVersion, model.SemanticVersion, model.Application with
        {
            Modules = [module with { Features = [feature with { Slices = [.. feature.Slices.Select(candidate => candidate.Id == slice.Id ? slice : candidate)] }] }]
        }));
    }
}
