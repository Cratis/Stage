// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticCratisAdmission.when_explaining_command_capabilities;

public class with_an_optional_composite_with_optional_null_passing_concepts : given.a_command_model
{
    void Establish() => SetModel(("concept Display : String\n  validate\n    length == 3\ntype Details\n  display Display optional\n" + Source).Replace("        produces InvoiceIssued", "        details Details optional\n        produces InvoiceIssued", StringComparison.Ordinal));
    void Because() => Evaluate();

    [Fact] void should_admit_the_optional_parent_and_member() => _diagnostics.ShouldBeEmpty();
}
