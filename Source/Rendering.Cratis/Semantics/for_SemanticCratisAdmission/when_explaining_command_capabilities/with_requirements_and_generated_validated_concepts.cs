// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticCratisAdmission.when_explaining_command_capabilities;

public class with_requirements_and_generated_validated_concepts : given.a_command_model
{
    void Establish() => SetModel(("concept Display : String\n  generate nanoid 8 alphabet \"ab\"\n  validate\n    min 2\n" + Source).Replace("        produces InvoiceIssued", "        generatedDisplay Display generated\n        display Display\n        validate\n          require description != \"draft\"\n        produces InvoiceIssued", StringComparison.Ordinal));
    void Because() => Evaluate();

    [Fact] void should_admit_the_request_concept_and_skip_the_generated_concept() => _diagnostics.ShouldBeEmpty();
}
