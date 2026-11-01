// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticCratisAdmission.when_explaining_command_capabilities;

public class with_requirements_and_optional_validated_concepts : given.a_command_model
{
    void Establish() => SetModel(("concept Count : Int\n  validate\n    > 5\n" + Source).Replace("        produces InvoiceIssued", "        count Count optional\n        validate\n          require description != \"draft\"\n        produces InvoiceIssued", StringComparison.Ordinal));
    void Because() => Evaluate();

    [Fact] void should_admit_the_absence_passing_scalar_concept() => _diagnostics.ShouldBeEmpty();
}
