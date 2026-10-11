// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution;
using Cratis.Stage.Contracts.Specifications.Semantic;
using Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.given;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor.when_running_protected_queries;

public class with_an_opaque_policy : a_projected_specification
{
    void Establish() => _plan = Compile(
        """
        specification AnOpaquePolicyNeedsATarget
          given caller
            authenticated
          when query InvoiceById
            invoiceId = "first"
          then denied
        """,
        """
        ```csharp
        return true;
        ```
        """);

    Task Because() => Run();

    [Fact] void should_not_treat_unsupported_as_a_denial() => _result.Outcome.ShouldEqual(SemanticSpecificationOutcome.Unsupported);
    [Fact] void should_name_the_authorization_capability() => _result.Unsupported!.Capability.ShouldEqual(StageExecutionCapability.Authorization);
    [Fact] void should_match_the_reference() => ((SemanticUnsupported)_reference.Execution).Capability.ShouldEqual(SemanticExecutionCapability.Authorization);
}
