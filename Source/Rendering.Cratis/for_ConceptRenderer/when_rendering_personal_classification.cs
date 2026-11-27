// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.Chronicle.Commands;
using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_ConceptRenderer.given;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Cratis.Stage.Rendering.Cratis.Renderers;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_ConceptRenderer;

public class when_rendering_personal_classification : concepts
{
    [Theory]
    [InlineData(null, SpecialCategory.None, false)]
    [InlineData(null, SpecialCategory.None, true)]
    [InlineData("racialOrEthnicOrigin", SpecialCategory.RacialOrEthnicOrigin, false)]
    [InlineData("politicalOpinions", SpecialCategory.PoliticalOpinions, false)]
    [InlineData("religiousOrPhilosophicalBeliefs", SpecialCategory.ReligiousOrPhilosophicalBeliefs, false)]
    [InlineData("tradeUnionMembership", SpecialCategory.TradeUnionMembership, false)]
    [InlineData("genetic", SpecialCategory.Genetic, false)]
    [InlineData("biometric", SpecialCategory.Biometric, false)]
    [InlineData("health", SpecialCategory.Health, false)]
    [InlineData("sexLifeOrSexualOrientation", SpecialCategory.SexLifeOrSexualOrientation, false)]
    [InlineData("racialOrEthnicOrigin", SpecialCategory.RacialOrEthnicOrigin, true)]
    [InlineData("politicalOpinions", SpecialCategory.PoliticalOpinions, true)]
    [InlineData("religiousOrPhilosophicalBeliefs", SpecialCategory.ReligiousOrPhilosophicalBeliefs, true)]
    [InlineData("tradeUnionMembership", SpecialCategory.TradeUnionMembership, true)]
    [InlineData("genetic", SpecialCategory.Genetic, true)]
    [InlineData("biometric", SpecialCategory.Biometric, true)]
    [InlineData("health", SpecialCategory.Health, true)]
    [InlineData("sexLifeOrSexualOrientation", SpecialCategory.SexLifeOrSexualOrientation, true)]
    public void should_compile_both_paths_with_the_declared_runtime_classification(string? category, SpecialCategory expected, bool criminal)
    {
        foreach (var secret in new[] { false, true })
        {
            var syntax = RenderSyntax(category, criminal, true, secret);
            var semantic = RenderSemantic(category, criminal, secret);
            var assembly = RenderedOutput.Load([syntax, semantic]);
            foreach (var name in new[] { "EmailAddress", "SemanticPersonal" })
            {
                var generated = assembly.GetTypes().Single(type => type.Name == name);
                var attribute = generated.GetCustomAttribute<PIIAttribute>()!;
                attribute.SpecialCategory.ShouldEqual(expected);
                attribute.Criminal.ShouldEqual(criminal);
                attribute.OperationalSecret.ShouldEqual(secret);
                generated.IsDefined(typeof(NotAuditedAttribute)).ShouldEqual(secret);
            }

            syntax.Content.ShouldNotContain("Encrypted");
            semantic.Content.ShouldNotContain("Encrypted");
            semantic.Content.ShouldNotContain("ComplianceDetails");
        }
    }

    [Fact]
    public void should_preserve_classification_when_syntax_details_are_disabled()
    {
        var file = RenderSyntax("health", true, false, true);
        file.Content.ShouldNotContain("ComplianceDetails");
        var generated = RenderedOutput.Load([file]).GetTypes().Single(type => type.Name == "EmailAddress");
        var attribute = generated.GetCustomAttribute<PIIAttribute>()!;
        attribute.SpecialCategory.ShouldEqual(SpecialCategory.Health);
        attribute.Criminal.ShouldBeTrue();
        attribute.OperationalSecret.ShouldBeTrue();
        generated.IsDefined(typeof(NotAuditedAttribute)).ShouldBeTrue();
    }

    [Fact]
    public void should_leave_unclassified_values_unmarked()
    {
        var builder = new CSharpCodeBuilder();
        ConceptComplianceRendering.Render(builder, null);
        builder.ToString().ShouldNotContain("[");
        var file = ConceptRenderer.Render(_emailAddress with { Attributes = [] }, _applicationSet, "Generated");
        var generated = RenderedOutput.Load([file]).GetTypes().Single(type => type.Name == "EmailAddress");
        generated.GetCustomAttribute<PIIAttribute>().ShouldBeNull();
        generated.IsDefined(typeof(NotAuditedAttribute)).ShouldBeFalse();
        file.Content.ShouldNotContain("Encrypted");
    }

    static RenderedFile RenderSemantic(string? category, bool criminal, bool secret)
    {
        var builder = new CSharpCodeBuilder().Namespace("Generated");
        ConceptComplianceRendering.Render(builder, new SemanticConceptCompliance(new(category, criminal), secret ? new(null) : null));
        builder.Line("public record SemanticPersonal(string Value) : global::Cratis.Concepts.ConceptAs<string>(Value);")
            .Line("public enum SpecialCategory { Unrelated }");
        return new("SemanticPersonal.cs", builder.ToString());
    }

    RenderedFile RenderSyntax(string? category, bool criminal, bool details, bool secret)
    {
        List<ConceptAttributeSyntax> attributes = [new("pii", SourceLocation.Start, "Classification reason.") { SpecialCategory = category, Criminal = criminal }];
        if (secret) attributes.Add(new("sensitive", SourceLocation.Start, "Ignored secret reason."));
        return ConceptRenderer.Render(_emailAddress with { Attributes = [.. attributes] }, _applicationSet, "Generated", complianceDetails: details);
    }
}
