// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticCratisAdmission.when_explaining_command_capabilities;

public class with_a_rule_on_named_concept_values : given.a_command_model
{
    void Establish() => SetModel(("concept Status : Enum\n  draft\n  issued\n" + Source).Replace("        produces InvoiceIssued", "        status Status\n        validate\n          status != \"draft\"\n        produces InvoiceIssued", StringComparison.Ordinal));
    void Because() => Evaluate();

    [Fact] void should_admit_the_named_member_comparison() => _diagnostics.ShouldBeEmpty();
}
