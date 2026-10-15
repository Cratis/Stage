// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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

    static ArtifactRenderDiagnostic[] Validate(string type, string value, bool rejection = false)
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
        var options = new CratisRenderingOptions("Inputs", "Inputs");
        var request = new ArtifactRenderRequest(model, SemanticExecutionPlan.Compile(model).Plan!, CratisRendering.CreateProfile(model.Application.Name, options), new(ArtifactRenderScopeKind.Application, model.Application.Id));
        var context = new SemanticApplicationContext(request, options);
        var diagnostics = new List<ArtifactRenderDiagnostic>();
        SemanticSpecificationAdmission.Validate(context, context.SelectedSlices().Single().Slice, diagnostics);
        return [.. diagnostics];
    }
}
