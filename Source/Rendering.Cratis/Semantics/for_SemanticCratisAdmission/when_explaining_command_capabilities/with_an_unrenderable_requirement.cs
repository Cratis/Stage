// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticCratisAdmission.when_explaining_command_capabilities;

public class with_an_unrenderable_requirement : given.a_command_model
{
    void Establish()
    {
        SetModel(Source.Replace("        produces InvoiceIssued", "        validate\n          require description != \"draft\"\n        produces InvoiceIssued", StringComparison.Ordinal));
        _command = _command with { Properties = [.. _command.Properties.Select(property => property.Name == "description" ? property with { Type = property.Type with { IsOptional = true } } : property)] };
    }
    void Because() => Evaluate();

    [Fact] void should_name_the_requirement_property_and_supported_shape() => Messages.ShouldContainOnly(["Command 'IssueInvoice' requirement #1 (description) cannot be rendered: the condition has no faithful generated guard. Use supported comparisons over non-optional scalar command properties without named concept values, or keep this shape unsupported (Cratis/Stage#409)."]);
    [Fact] void should_keep_the_code() => _diagnostics.Single().Code.ShouldEqual("STAGE-ESM-005");
}
