// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticValidationRendering;

public class when_rendering_checksum_severity
{
    [Theory]
    [InlineData(SemanticValidationSeverity.Error, "")]
    [InlineData(SemanticValidationSeverity.Warning, ".WithSeverity(global::Cratis.Arc.Validation.ValidationResultSeverity.Warning)")]
    [InlineData(SemanticValidationSeverity.Information, ".WithSeverity(global::Cratis.Arc.Validation.ValidationResultSeverity.Information)")]
    public void should_keep_the_authored_message_and_severity(SemanticValidationSeverity severity, string suffix)
    {
        var rule = new SemanticValidationRule(default, SemanticValidationRuleKind.Checksum, SemanticValue.Text("luhn"), "Invalid check digit") { Severity = severity };
        var builder = new CSharpCodeBuilder();
        SemanticValidationRendering.Render(builder, rule, "reference", SemanticPrimitiveType.Text, collection: false, concept: true);
        builder.ToString().ShouldContain($".WithMessage(\"Invalid check digit\"){suffix};");
    }
}
