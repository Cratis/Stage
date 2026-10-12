// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Specifications.Comparison;

namespace Cratis.Stage.Specifications;

public sealed partial class SemanticSpecificationExecutor
{
    internal static SemanticSpecificationRunRecord Rejected(SemanticSlice slice, SemanticSpecification specification, string message, string? code = null, bool denied = false)
    {
        var failures = SemanticExpectationComparer.Compare(specification, [], [], message, code, denied);

        return Record(slice, specification, failures.Count == 0 ? SemanticSpecificationOutcome.Passed : SemanticSpecificationOutcome.Failed, "Rejected", failures: failures, trace: new SemanticExecutionTrace([], new Dictionary<string, string>(), new Dictionary<string, string>(), message) { RejectionCode = code });
    }

    internal static SemanticSpecificationRunRecord Record(SemanticSlice slice, SemanticSpecification specification, SemanticSpecificationOutcome outcome, string? kind = null, SemanticUnsupportedCapability? unsupported = null, IReadOnlyList<string>? failures = null, SemanticExecutionTrace? trace = null) =>
        new(specification.Id.ToString(), specification.Name, slice.Id.ToString(), slice.Kind.ToString(), outcome, kind, unsupported, failures ?? [], trace);
}
