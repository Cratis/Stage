// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Renderers;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CSharpCodeBuilder;

public class when_refusing_authored_validation_bodies : Specification
{
    [Theory]
    [InlineData("java", "return true;", false)]
    [InlineData("java", "return true;", true)]
    [InlineData("csharp", "#if true\nreturn true;\n#endif", false)]
    [InlineData("csharp", "#if true\nreturn true;\n#endif", true)]
    void should_refuse_foreign_or_unsupported_concept_validation_at_the_renderer(string language, string body, bool codeBlock)
    {
        var location = SourceLocation.Start;
        var code = new CodeBlockSyntax(language, body, location);
        ValidateSyntax validation = codeBlock ? new CodeValidateSyntax(code, location) :
            new DeclarativeValidateSyntax([new(ValidationRuleSyntax.ConceptValue, ValidationRuleKind.Rule, null, null, location, Code: code)], location);
        var concept = new ConceptSyntax("Reference", "String", [], [], location, Validations: [validation]);
        var error = Assert.Throws<InlineCodeRejected>(() => ConceptRenderer.Render(concept, new ApplicationSet([]), "Shop"));
        Assert.DoesNotContain(error.Diagnostics, diagnostic => diagnostic.Code == "STAGE-DEGRADE-003");
    }

    [Theory]
    [InlineData("java", "return true;")]
    [InlineData("csharp", "#if true\nreturn true;\n#endif")]
    void should_refuse_foreign_or_unsupported_command_validation_at_the_renderer(string language, string body)
    {
        var location = SourceLocation.Start;
        var code = new CodeBlockSyntax(language, body, location);
        var command = new CommandSyntax(
            "Register",
            [new("reference", new("String", false, false, location), location)],
            null,
            [new DeclarativeValidateSyntax([new("reference", ValidationRuleKind.Rule, null, null, location, Code: code)], location)],
            [],
            null,
            location);
        var builder = new CSharpCodeBuilder();
        CommandValidatorRenderer.Render(builder, command, "Register", new ApplicationSet([]), []);
        var error = Assert.Throws<InlineCodeRejected>(builder.ToString);
        Assert.DoesNotContain(error.Diagnostics, diagnostic => diagnostic.Code == "STAGE-DEGRADE-003");
    }
}
