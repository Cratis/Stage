// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Specifications.Comparison;

namespace Cratis.Stage.Specifications;

public sealed partial class SemanticSpecificationExecutor
{
    internal static SemanticSpecificationRunRecord Rejected(SemanticSlice slice, SemanticSpecification specification, string message) =>
        Rejected(slice, specification, new SemanticRejection(message));

    internal static SemanticSpecificationRunRecord Rejected(SemanticSlice slice, SemanticSpecification specification, SemanticRejection rejection)
    {
        var failures = SemanticExpectationComparer.Compare(specification, new([], []), rejection);
        var trace = new SemanticExecutionTrace([], new Dictionary<string, string>(), new Dictionary<string, string>(), rejection.Message)
        {
            RejectionCode = rejection.Code,
            RejectionCategory = specification.When is null && !specification.ThenQueries.IsEmpty ? rejection.Category.ToString() : null
        };

        return Settled(slice, specification, failures, trace);
    }

    internal static SemanticSpecificationRunRecord Settled(SemanticSlice slice, SemanticSpecification specification, IReadOnlyList<string> failures, SemanticExecutionTrace trace) =>
        Create(slice, specification, failures.Count == 0 ? SemanticSpecificationOutcome.Passed : SemanticSpecificationOutcome.Failed) with
        {
            ExecutionKind = trace.Rejection is null ? "Accepted" : "Rejected",
            Failures = failures,
            Trace = trace
        };

    internal static SemanticSpecificationRunRecord Unsupported(SemanticSlice slice, SemanticSpecification specification, SemanticUnsupportedCapability unsupported) =>
        Create(slice, specification, SemanticSpecificationOutcome.Unsupported) with { Unsupported = unsupported };

    internal static SemanticSpecificationRunRecord Failed(SemanticSlice slice, SemanticSpecification specification, IReadOnlyList<string> failures) =>
        Create(slice, specification, SemanticSpecificationOutcome.Failed) with { Failures = failures };

    internal static SemanticSpecificationRunRecord Cancelled(SemanticSlice slice, SemanticSpecification specification) =>
        Create(slice, specification, SemanticSpecificationOutcome.Cancelled);

    static SemanticSpecificationRunRecord Create(SemanticSlice slice, SemanticSpecification specification, SemanticSpecificationOutcome outcome) =>
        new(specification.Id.ToString(), specification.Name, slice.Id.ToString(), slice.Kind.ToString(), outcome, null, null, [], null);
}
