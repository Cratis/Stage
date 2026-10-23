// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rules;
using Cratis.Stage.Contracts.Screenplay;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Validation.for_ValidationRuleRenderer;

public class when_carrying_authored_patterns_to_clients : Specification
{
    [Theory]
    [InlineData(@"^\w+\s*$")]
    [InlineData("^(a{2996}b)+$")]
    [InlineData("(){999999999999999999999999999}")]
    public void should_keep_the_authored_pattern_while_the_server_uses_a_cached_matcher(string pattern)
    {
        var rule = new ValidationRuleSyntax("value", ValidationRuleKind.Matches, new LiteralExpressionSyntax(pattern, SourceLocation.Start), null, SourceLocation.Start);
        var client = ValidationRuleConverter.Convert([new DeclarativeValidateSyntax([rule], SourceLocation.Start)]);
        client.Single().Rules.OfType<Matches>().Single().Pattern.ShouldEqual(pattern);
        var builder = new CSharpCodeBuilder();
        ValidationRuleRenderer.RenderMatchFields(builder, [rule, rule]);
        builder.ToString().Split("PortableMatcher.Compile(", StringSplitOptions.None).Length.ShouldEqual(2);
        builder.ToString().ShouldContain(CSharpCodeBuilder.StringLiteral(pattern));
        ValidationRuleRenderer.RenderCall(rule.Rule, CSharpCodeBuilder.StringLiteral(pattern)).ShouldNotContain("Compile(");
    }
}
