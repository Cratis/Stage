// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_SourceLineDirectives;

public class when_relocating_directives : Specification
{
    static readonly DocumentId _document = DocumentId.Create("Invoices.play");

    static string Body(string path) => $"class A\n{{\n#line 7 \"{path}\"\n    int B() => 1;\n#line default\n}}\n";

    static string Relocate(ArtifactRenderProfile profile, string artifactPath, string text)
    {
        _ = SourceLineMappingInput.TryRead(profile, out var mapping);
        var artifact = PlannedArtifact.CreateText(artifactPath, text);
        return Encoding.UTF8.GetString([.. SourceLineDirectives.Relocate(profile, mapping, [artifact]).Single().Bytes]);
    }

    static ArtifactRenderProfile Profile(bool service = false) =>
        CratisRendering.WithSourceLineMapping(CratisRendering.CreateProfile("Projects", new("Projects", "Projects") { ServiceLayout = service }), "Model", [KeyValuePair.Create(_document, "Invoices.play")]);

    [Fact] void should_not_climb_for_a_file_at_the_root() => Relocate(Profile(), "A.cs", Body("Model/Invoices.play")).ShouldEqual(Body("Model/Invoices.play"));
    [Fact] void should_climb_one_folder_per_directory() => Relocate(Profile(), "Billing/Invoices/A.cs", Body("Model/Invoices.play")).ShouldEqual(Body("../../Model/Invoices.play"));
    [Fact] void should_climb_three_folders_at_depth_three() => Relocate(Profile(), "a/b/c/A.cs", Body("Model/Invoices.play")).ShouldEqual(Body("../../../Model/Invoices.play"));
    [Fact] void should_add_the_service_layout_prefix() => Relocate(Profile(true), "Billing/A.cs", Body("Model/Invoices.play")).ShouldEqual(Body("../../../Model/Invoices.play"));
    [Fact] void should_leave_an_unknown_path_untouched() => Relocate(Profile(), "a/A.cs", Body("Other/x.play")).ShouldEqual(Body("Other/x.play"));

    [Fact]
    void should_return_the_same_instance_for_a_file_without_directives()
    {
        _ = SourceLineMappingInput.TryRead(Profile(), out var mapping);
        var artifact = PlannedArtifact.CreateText("a/A.cs", "class A;\n");
        Assert.Same(artifact, SourceLineDirectives.Relocate(Profile(), mapping, [artifact]).Single());
    }
}
