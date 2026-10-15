// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticSpecificationAdmission;

public class when_validating_composite_fixture_values : Specification
{
    ArtifactRenderDiagnostic[] _composite = [];
    ArtifactRenderDiagnostic[] _collection = [];
    ArtifactRenderDiagnostic[] _omitted = [];
    ArtifactRenderDiagnostic[] _empty = [];
    ArtifactRenderDiagnostic[] _nestedCollection = [];
    ArtifactRenderDiagnostic[] _nestedCollectionInElement = [];
    ArtifactRenderDiagnostic[] _reorderedAcceptance = [];
    ArtifactRenderDiagnostic[] _reorderedRejection = [];
    ArtifactRenderDiagnostic[] _reorderedNestedRejection = [];
    ArtifactRenderDiagnostic[] _reorderedCollectionRejection = [];
    ArtifactRenderDiagnostic[] _orderedSparseRejection = [];
    ArtifactRenderDiagnostic[] _explicitNull = [];
    ArtifactRenderDiagnostic[] _nestedNull = [];
    ArtifactRenderDiagnostic[] _nullInCollectionElement = [];

    void Because()
    {
        _composite = Validate("Details", "{\"title\":\"Title\",\"next\":{\"title\":\"Next\"}}");
        _collection = Validate("Details[]", "[{\"title\":\"First\"},{\"title\":\"Second\"}]");
        _omitted = Validate("Details", "{}");
        _empty = Validate("Details[]", "[]");
        _nestedCollection = Validate("Details", "{\"items\":[\"one\"]}");
        _nestedCollectionInElement = Validate("Details[]", "[{\"items\":[\"one\"]}]");
        _reorderedAcceptance = Validate("Details", "{\"next\":{},\"title\":\"Title\"}");
        _reorderedRejection = Validate("Details", "{\"next\":{},\"title\":\"Title\"}", rejection: true);
        _reorderedNestedRejection = Validate("Details", "{\"title\":\"Root\",\"next\":{\"next\":{},\"title\":\"Nested\"}}", rejection: true);
        _reorderedCollectionRejection = Validate("Details[]", "[{\"next\":{},\"title\":\"Title\"}]", rejection: true);
        _orderedSparseRejection = Validate("Details", "{\"next\":{}}", rejection: true);
        _explicitNull = Validate("Details", "{\"title\":\"Title\"}", explicitNullMembers: true);
        _nestedNull = Validate("Details", "{\"next\":{\"title\":\"Nested\"}}", explicitNullMembers: true);
        _nullInCollectionElement = Validate("Details[]", "[{\"title\":\"Title\"}]", explicitNullMembers: true);
    }

    [Fact] void should_admit_nested_scalar_composites() => _composite.ShouldBeEmpty();
    [Fact] void should_admit_collections_of_composites() => _collection.ShouldBeEmpty();
    [Fact] void should_admit_omitted_optional_members_rendered_as_null() => _omitted.ShouldBeEmpty();
    [Fact] void should_preserve_empty_collection_admission() => _empty.ShouldBeEmpty();
    [Fact] void should_refuse_collection_members_that_record_equality_cannot_compare() => _nestedCollection.Single().Code.ShouldEqual("STAGE-ESM-011");
    [Fact] void should_refuse_collection_members_inside_collection_elements() => _nestedCollectionInElement.Single().Code.ShouldEqual("STAGE-ESM-011");

    [Fact] void should_preserve_reordered_acceptance_fixtures() => _reorderedAcceptance.ShouldBeEmpty();
    [Fact] void should_refuse_reordered_rejection_inputs() => _reorderedRejection.Single().Code.ShouldEqual("STAGE-ESM-011");
    [Fact] void should_refuse_reordered_nested_rejection_inputs() => _reorderedNestedRejection.Single().Code.ShouldEqual("STAGE-ESM-011");
    [Fact] void should_refuse_reordered_collection_elements_in_rejection_inputs() => _reorderedCollectionRejection.Single().Code.ShouldEqual("STAGE-ESM-011");
    [Fact] void should_admit_ordered_rejection_inputs_omitting_optional_members() => _orderedSparseRejection.ShouldBeEmpty();

    [Fact] void should_refuse_explicit_null_members_in_typed_inputs() => _explicitNull.Single().Code.ShouldEqual("STAGE-ESM-011");
    [Fact] void should_refuse_explicit_null_members_in_nested_typed_inputs() => _nestedNull.Single().Code.ShouldEqual("STAGE-ESM-011");
    [Fact] void should_refuse_explicit_null_members_in_collection_elements() => _nullInCollectionElement.Single().Code.ShouldEqual("STAGE-ESM-011");

    static ArtifactRenderDiagnostic[] Validate(string type, string value, bool rejection = false, bool explicitNullMembers = false)
    {
        var model = invoice_model.Compile($$"""
            type Details
              title String optional
              next Details optional
              items String[] optional
            module Inputs
              feature Optional
                slice StateChange Check
                  command Check
                    id String
                    value {{type}}
                    returns id
                  specification Checking
                    when Check
                      id = "one"
                      value = {{value}}
                    {{(rejection ? "then error \"Rejected\"" : "then returns \"one\"")}}
            """);
        if (explicitNullMembers)
        {
            var module = model.Application.Modules.Single();
            var feature = module.Features.Single();
            var slice = feature.Slices.Single();
            var specification = slice.Specifications.Single();
            var when = specification.When!;
            var changed = specification with
            {
                When = when with { Values = [.. when.Values.Select(property => property.Value is SemanticCompositeValue or SemanticArrayValue
                    ? property with { Value = WithNullMembers(property.Value) } : property)] }
            };
            model = ExecutableSemanticModel.Create(model.LanguageVersion, model.SemanticVersion, model.Application with
            {
                Modules = [module with { Features = [feature with { Slices = [slice with { Specifications = [changed] }] }] }]
            });
        }

        var options = new CratisRenderingOptions("Inputs", "Inputs");
        var request = new ArtifactRenderRequest(model, SemanticExecutionPlan.Compile(model).Plan!, CratisRendering.CreateProfile(model.Application.Name, options), new(ArtifactRenderScopeKind.Application, model.Application.Id));
        var context = new SemanticApplicationContext(request, options);
        var diagnostics = new List<ArtifactRenderDiagnostic>();
        SemanticSpecificationAdmission.Validate(context, context.SelectedSlices().Single().Slice, diagnostics);
        return [.. diagnostics];
    }

    // Source fixtures reject null literals; typed fixtures can still supply explicit null members.
    static SemanticValue WithNullMembers(SemanticValue value) => value switch
    {
        SemanticCompositeValue composite => SemanticValue.Composite([.. composite.Properties.Select(property =>
            property with { Value = WithNullMembers(property.Value) })]),
        SemanticArrayValue array => SemanticValue.Array([.. array.Values.Select(WithNullMembers)]),
        _ => SemanticValue.Null
    };
}
