// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_rendering_required_filters_on_optional_properties : Specification
{
    string _query = null!;

    void Because()
    {
        var plan = invoice_model.Plan(invoice_model.Compile(a_query_filter_model.OptionalPropertySource));
        Assert.True(plan.Success, string.Join("; ", plan.Diagnostics.Select(diagnostic => $"{diagnostic.Code}: {diagnostic.Message}")));
        _query = Encoding.UTF8.GetString(plan.Artifacts.Single(artifact => artifact.RelativePath.EndsWith("Lookup.cs", StringComparison.Ordinal)).Bytes.AsSpan());
    }

    [Fact] void should_keep_required_filter_parameters_non_nullable() => _query.ShouldContain("FilteredOptionalProperties(IReadModels readModels, bool flag, Category category)");
    [Fact] void should_lift_the_value_type_comparer() => _query.ShouldContain("EqualityComparer<bool?>.Default.Equals(instance.Flag, flag)");
    [Fact] void should_lift_the_concept_comparer() => _query.ShouldContain("EqualityComparer<Category?>.Default.Equals(instance.Category, category)");
}
