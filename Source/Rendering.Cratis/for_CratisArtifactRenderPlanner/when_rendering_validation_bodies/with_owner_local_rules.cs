// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.when_rendering_validation_bodies;

/// <summary>Cratis/Stage#850: the authored validation bodies live in a sibling static class directly after their validator, with their text unchanged.</summary>
public class with_owner_local_rules : Specification
{
    const string Body = "return context.Value > 0 && context.IsWholeArtifact;";

    IReadOnlyList<(string Path, string Text)> _files = [];

    void Because()
    {
        var plan = opaque_policy_model.Plan(validation_body_model.Load(validation_body_model.Concept(Body)));
        Assert.True(plan.Success, opaque_policy_model.Errors(plan));
        _files = [.. plan.Artifacts.Select(artifact => (artifact.RelativePath, Encoding.UTF8.GetString(artifact.Bytes.AsSpan())))];
    }

    [Fact] void should_not_produce_the_shared_validation_bodies_file() => _files.ShouldNotContain(file => file.Path.EndsWith("ValidationBodies.cs", StringComparison.Ordinal));

    [Fact] void should_place_the_rules_class_in_the_validator_file() =>
        RulesFiles().ShouldContain(file => file.Text.Contains("class AmountValidator", StringComparison.Ordinal));

    [Fact] void should_declare_the_rules_class_as_a_sibling_after_the_validator()
    {
        var root = CSharpSyntaxTree.ParseText(RulesFiles().Single().Text).GetRoot();
        var types = root.DescendantNodes().OfType<ClassDeclarationSyntax>().ToArray();
        var rules = types.Single(type => type.Identifier.ValueText.EndsWith("ValidationRules", StringComparison.Ordinal));
        rules.Parent.ShouldEqual(types.Single(type => type.Identifier.ValueText.EndsWith("Validator", StringComparison.Ordinal)).Parent);
        (rules.SpanStart > types.Single(type => type.Identifier.ValueText.EndsWith("Validator", StringComparison.Ordinal)).SpanStart).ShouldBeTrue();
    }

    [Fact] void should_keep_the_authored_body_text_unchanged() =>
        RulesFiles().Single().Text.ShouldContain(Body);

    IEnumerable<(string Path, string Text)> RulesFiles() => _files.Where(file => file.Text.Contains("ValidationRules", StringComparison.Ordinal) && file.Text.Contains("static class", StringComparison.Ordinal));
}
#endif
