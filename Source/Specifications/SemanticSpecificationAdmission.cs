// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Execution;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Semantics;

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
    public static SemanticUnsupportedCapability? Check(SemanticExecutionPlan plan, SemanticSpecification specification) => Check(plan, specification, null);

    /// <summary>Checks a specification with renderer-admitted named rule execution.</summary>
    /// <param name="plan">The executable semantic plan.</param>
    /// <param name="specification">The specification to check.</param>
    /// <param name="bodies">The optional compiled bodies.</param>
    /// <returns>The first typed refusal, or null.</returns>
    public static SemanticUnsupportedCapability? Check(SemanticExecutionPlan plan, SemanticSpecification specification, SemanticBodyExecution? bodies) =>
        SemanticRunAdmission.Check(plan, specification, bodies);
}
