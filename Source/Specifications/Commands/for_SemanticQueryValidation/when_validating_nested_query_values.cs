// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Stage.Specifications.Commands.for_SemanticQueryValidation;

public class when_validating_nested_query_values : Specification
{
    SemanticExecutionPlan _plan = null!;
    SemanticKeyedQuery _query = null!;
    SemanticSpecificationQueryResult _request = null!;
    SemanticValidationFailure[] _failures = [];

    void Establish()
    {
        const string source = """
            concept Code : String
              validate
                min 2
            type Pair
              first Code
              rest Code[]
            module Lookup
              feature Codes
                slice StateView View
                  readmodel Summary
                    id String
                  query GetSummary => Summary optional
                    by id String
            """;
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("NestedQueries"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("model"), "model", "model.play", source);
        var compiled = new SemanticModelCompiler().Compile("NestedQueries", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compiled.Success, string.Join("; ", compiled.Diagnostics.Select(diagnostic => diagnostic.Message)));
        _plan = SemanticExecutionPlan.Compile(compiled.Value!.Model).Plan!;
        var composite = _plan.Model.Application.Types.Single();
        var query = _plan.Queries.Values.Single();
        _query = query with { Argument = query.Argument! with { Type = new(SemanticTypeReferenceKind.CompositeType, SemanticPrimitiveType.Unknown, composite.Id, false, false) } };
        var first = composite.Properties.Single(property => property.Name == "first");
        var rest = composite.Properties.Single(property => property.Name == "rest");
        _request = new(query.Id, new SemanticCompositeValue([new(first.Id, SemanticValue.Text("x")), new(rest.Id, new SemanticArrayValue([SemanticValue.Text(""), SemanticValue.Text("valid"), SemanticValue.Text("a")]))]), []);
    }

    void Because() => _failures = [.. SemanticQueryValidation.Failures(_plan, _query, _request)];

    [Fact] void should_validate_the_scalar_member_and_each_collection_element() => _failures.Length.ShouldEqual(3);
    [Fact] void should_use_the_reference_default_message() => _failures.Select(failure => failure.Message).ShouldContainOnly("A value must be at least 2 characters long.", "A value must be at least 2 characters long.", "A value must be at least 2 characters long.");
}
