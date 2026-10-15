// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticCratisAdmission.when_explaining_command_capabilities;

public class with_unsupported_named_value_requirements : given.a_command_model
{
    [Theory]
    [InlineData("optional")]
    [InlineData("different-concept")]
    [InlineData("ordering")]
    public void should_explain_unsupported_enum_requirement_forms(string form)
    {
        var other = form == "different-concept" ? "OtherStatus" : "Status";
        SetModel(("concept Status : Enum\n  draft\n  issued\nconcept OtherStatus : Enum\n  draft\n  issued\n" + Source).Replace("        produces InvoiceIssued", $"        status Status\n        other {other}\n        validate\n          require status == other\n        produces InvoiceIssued", StringComparison.Ordinal));

        // The ESM contract rejects optional conditions before admission; retain a defensive refusal.
        if (form == "optional")
        {
            _command = _command with { Properties = [.. _command.Properties.Select(property => property.Name == "status" ? property with { Type = property.Type with { IsOptional = true } } : property)] };
        }
        else if (form == "ordering")
        {
            _command = _command with { Requirements = [_command.Requirements.Single() with { Condition = ((SemanticComparison)_command.Requirements.Single().Condition) with { Operator = SemanticComparisonOperator.GreaterThan } }] };
        }
        Evaluate();
        _diagnostics.Single().Code.ShouldEqual("STAGE-ESM-005");
        Messages.Single().ShouldContain("Optional operands, collections, different concepts, and ordering remain unsupported (Cratis/Stage#420).");
    }
}
