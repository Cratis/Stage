// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticCratisAdmission.when_explaining_command_capabilities;

public class with_an_optional_collection_of_null_passing_concepts : given.a_command_model
{
    void Establish() => SetModel(("concept Display : String\n  validate\n    length == 3\n" + Source).Replace("        produces InvoiceIssued", "        displays Display[] optional\n        produces InvoiceIssued", StringComparison.Ordinal));
    void Because() => Evaluate();

    [Fact] void should_refuse_collections_despite_null_passing_element_rules() => Messages.Single().ShouldContain("Collections and named-value concepts remain unsupported");
    [Fact] void should_keep_the_code() => _diagnostics.Single().Code.ShouldEqual("STAGE-ESM-005");
}
