// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Specifications.Admission;

namespace Cratis.Stage.Specifications;

/// <summary>
/// Checks whether the shared executor can run a specification without dropping modeled behavior.
/// </summary>
public static class SemanticSpecificationAdmission
{
    /// <summary>
    /// Returns the first unsupported capability, or null when the specification is admitted.
    /// </summary>
    /// <param name="plan">The executable semantic plan.</param>
    /// <param name="specification">The specification to check.</param>
    /// <returns>The typed refusal, or null when execution is admitted.</returns>
    public static SemanticUnsupportedCapability? Check(SemanticExecutionPlan plan, SemanticSpecification specification) =>
        SemanticRunAdmission.Check(plan, specification);
}
