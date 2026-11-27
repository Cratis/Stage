// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Cratis.Stage.Rendering.Cratis.Naming.Imports;
using Cratis.Stage.Rendering.Cratis.Renderers;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.CodeGeneration.for_CSharpImports;

public class when_rendering_a_special_category_concept : Specification
{
    CSharpCodeBuilder _builder = null!;
    string _source = null!;

    void Establish()
    {
        _builder = new CSharpCodeBuilder(GeneratedProjects.Application, [], false).Namespace("Generated");
        ConceptComplianceRendering.Render(_builder, new SemanticConceptCompliance(new("health", true), null));
        _builder.Line("public record HealthInformation(string Value) : global::Cratis.Concepts.ConceptAs<string>(Value);");
    }

    void Because() => _source = _builder.ToString();

    [Fact] void should_import_the_special_category_namespace() => _source.ShouldContain("using Cratis.Chronicle.Compliance.GDPR;");
    [Fact] void should_spell_the_classification_with_short_names() => _source.ShouldContain("[PII(SpecialCategory = SpecialCategory.Health, Criminal = true)]");
    [Fact] void should_not_leave_qualified_references() => _source.ShouldNotContain("global::");
    [Fact] void should_compile_the_imported_concept() => RenderedOutput.Errors([new RenderedFile("HealthInformation.cs", _source)]).ShouldBeEmpty();
}
