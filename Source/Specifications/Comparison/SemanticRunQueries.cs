// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Stage.Specifications.Comparison;

internal static class SemanticRunQueries
{
    internal static SemanticRunProjections.Projected[] Select(
        SemanticExecutionPlan plan,
        SemanticSpecificationQueryResult assertion,
        IEnumerable<SemanticRunProjections.Projected> projected)
    {
        var query = plan.Queries[assertion.Query];

        return [.. projected.Where(row => row.ReadModel == query.ReadModel &&
            (!query.KeyParts.IsDefaultOrEmpty && query.KeyParts.Length > 1
                ? SemanticExpectationComparer.AreEqual(row.Key, assertion.Key)
                : query.KeyProperty is null || Matches(row, query.KeyProperty.Value, assertion.Key)) &&
            assertion.Filters.All(filter => Matches(row, query.Filters.Single(candidate => candidate.Id == filter.Filter).Property, filter.Value)))];
    }

    static bool Matches(SemanticRunProjections.Projected row, SemanticId property, SemanticValue value) =>
        row.Values.Any(candidate => candidate.TargetProperty == property && SemanticExpectationComparer.AreEqual(candidate.Value, value));
}
