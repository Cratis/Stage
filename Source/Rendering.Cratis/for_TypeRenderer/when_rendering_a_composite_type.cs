// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Renderers;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_TypeRenderer;

public class when_rendering_a_composite_type : Specification
{
    ApplicationSet _applicationSet = null!;
    TypeSyntax _address = null!;
    RenderedFile _file = null!;

    void Establish()
    {
        var postalCodeConcept = new ConceptSyntax("PostalCode", "String", [], [], SourceLocation.Start);

        _address = new TypeSyntax(
            "Address",
            [
                new PropertySyntax("street", new TypeRefSyntax("String", false, false, SourceLocation.Start), SourceLocation.Start),
                new PropertySyntax("postalCode", new TypeRefSyntax("PostalCode", false, false, SourceLocation.Start), SourceLocation.Start),
            ],
            SourceLocation.Start);

        var application = new ApplicationSyntax([], [postalCodeConcept], [], [], SourceLocation.Start, Types: [_address]);
        _applicationSet = new ApplicationSet([application]);
    }

    void Because() => _file = TypeRenderer.Render(_address, _applicationSet, "CratisApp");

    [Fact] void should_place_the_file_under_common_when_unused_by_any_slice() => _file.RelativePath.ShouldEqual(Path.Combine("Common", "Address.cs"));
    [Fact] void should_declare_the_namespace() => _file.Content.ShouldContain("namespace CratisApp.Common;");
    [Fact] void should_declare_the_record_with_plain_and_concept_properties() =>
        _file.Content.ShouldContain("public record Address(string Street, PostalCode PostalCode);");
}
