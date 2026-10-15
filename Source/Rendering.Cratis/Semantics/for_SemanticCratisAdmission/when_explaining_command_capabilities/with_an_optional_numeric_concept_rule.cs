// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticCratisAdmission.when_explaining_command_capabilities;

public class with_an_optional_numeric_concept_rule : given.a_command_model
{
    void Establish() => SetModel(("concept Count : Int\n" + Source).Replace("        produces InvoiceIssued", "        count Count?\n        validate\n          count > 0\n        produces InvoiceIssued", StringComparison.Ordinal));
    void Because() => Evaluate();

    [Fact] void should_admit_the_absence_passing_scalar_comparison() => _diagnostics.ShouldBeEmpty();
}
