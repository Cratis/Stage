// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Semantics;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_mapping_generated_member_casing_errors : Specification
{
    const string Body = "var payload = context.Event;\nreturn new Total(payload.Id, payload.amount);";
    LoadedSemanticModel _inline;
    LoadedSemanticModel _file;
    LoadedSemanticModel _conditional;
    LoadedSemanticModel _unknown;
    ArtifactRenderDiagnostic _inlineError;
    ArtifactRenderDiagnostic _fileError;
    ArtifactRenderDiagnostic _conditionalError;
    ArtifactRenderDiagnostic _unknownError;

    async Task Establish()
    {
        var source = when_rendering_a_pure_reducer.Source.Replace(
            "return new Total(Guid.Parse(\"00000000-0000-0000-0000-000000000001\"), context.Event.Amount);",
            Body.Replace("\n", "\n          ", StringComparison.Ordinal),
            StringComparison.Ordinal);
        _inline = await when_rendering_a_pure_reducer.Load(source);
        _conditional = await when_rendering_a_pure_reducer.Load(source.Replace(
            Body.Replace("\n", "\n          ", StringComparison.Ordinal), "return new Total(context.Event.Id, context.State?.amount ?? 0m);", StringComparison.Ordinal));
        _unknown = await when_rendering_a_pure_reducer.Load(source.Replace("payload.amount", "payload.missing", StringComparison.Ordinal));
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, ".git")) && !Directory.Exists(Path.Combine(root.FullName, ".git"))) root = root.Parent;
        Assert.NotNull(root);
        var folder = Path.Combine(root.FullName, ".ai-work", "casing-fixtures", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            var fileSource = source.Replace($"```csharp\n          {Body.Replace("\n", "\n          ", StringComparison.Ordinal)}\n          ```", "file Fold.cs", StringComparison.Ordinal);
            Assert.NotEqual(source, fileSource);
            await File.WriteAllTextAsync(Path.Combine(folder, "Orders.play"), fileSource);
            await File.WriteAllTextAsync(Path.Combine(folder, "Fold.cs"), Body);
            _file = await SemanticModelLoader.LoadFromPathAsync(folder, null, "Projects");
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    void Because()
    {
        _inlineError = when_rendering_a_pure_reducer.Plan(_inline).Diagnostics.Single(error => error.Code == "STAGE-ESM-019");
        _fileError = when_rendering_a_pure_reducer.Plan(_file).Diagnostics.Single(error => error.Code == "STAGE-ESM-019");
        _conditionalError = when_rendering_a_pure_reducer.Plan(_conditional).Diagnostics.Single(error => error.Code == "STAGE-ESM-019");
        _unknownError = when_rendering_a_pure_reducer.Plan(_unknown).Diagnostics.Single(error => error.Code == "STAGE-ESM-019");
    }

    [Fact] void should_name_the_generated_member_for_an_inline_body() => _inlineError.Message.ShouldContain("Use generated member 'Amount' instead of authored name 'amount'.");
    [Fact] void should_name_the_generated_member_for_a_file_body() => _fileError.Message.ShouldContain("Use generated member 'Amount' instead of authored name 'amount'.");
    [Fact] void should_map_to_the_authored_inline_line_and_column()
    {
        var requirement = _inline.ImplementationRequirements.Single();
        var line = requirement.BodyLines[1];
        _inlineError.Message.ShouldContain($"{requirement.Source.Span.Document}:{line.Line}:{line.Column + "return new Total(payload.Id, payload.".Length}");
    }
    [Fact] void should_map_to_the_attachment_line_and_column() => _fileError.Message.ShouldContain("Fold.cs:2:38:");
    [Fact] void should_name_the_generated_member_for_a_conditional_access() => _conditionalError.Message.ShouldContain("Use generated member 'Amount' instead of authored name 'amount'.");
    [Fact] void should_not_invent_a_generated_member_for_an_unrelated_compile_error() => _unknownError.Message.ShouldNotContain("Use generated member");
    [Fact] void should_preserve_the_inline_body() => _inline.ImplementationContents.Single().Value.ShouldEqual(Body);
    [Fact] void should_preserve_the_file_body() => _file.ImplementationContents.Single().Value.ShouldEqual(Body);
}
#endif
