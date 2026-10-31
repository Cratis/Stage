// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

public class when_reporting_compiler_errors_at_play_lines
{
    static readonly MetadataReference[] References = [.. ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") ?? string.Empty)
        .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries).Select(path => MetadataReference.CreateFromFile(path))];

    [Fact]
    void should_report_an_error_in_a_multi_line_body_at_its_play_line()
    {
        var loaded = opaque_policy_model.Load(opaque_policy_model.Source("var staff = context.Identity.IsAuthenticated;\nvar allowed = staff && context.Subject != \"\";\nreturn allowed;"));
        var plan = a_source_line_mapped_plan.Plan(loaded);
        var requirement = a_source_line_mapped_plan.InlineRequirements(loaded).Single();
        var (artifact, lines, index) = a_source_line_mapped_plan.Find(plan, requirement);

        // The second authored line follows the directive and the first authored line.
        Assert.Contains("staff &&", lines[index + 2], StringComparison.Ordinal);
        lines[index + 2] = lines[index + 2].Replace("staff &&", "__stage_undefined &&", StringComparison.Ordinal);
        var tree = CSharpSyntaxTree.ParseText(string.Join('\n', lines), path: artifact.RelativePath);
        var diagnostic = CSharpCompilation.Create("MappedLines", [tree], References, new(OutputKind.DynamicallyLinkedLibrary)).GetDiagnostics()
            .Single(candidate => candidate.Id == "CS0103" && candidate.GetMessage().Contains("__stage_undefined", StringComparison.Ordinal));

        var mapped = diagnostic.Location.GetMappedLineSpan();
        Assert.Equal(a_source_line_mapped_plan.Directive(artifact, requirement).Split('"')[1], mapped.Path);
        Assert.Equal(requirement.BodyLines[1].Line, mapped.StartLinePosition.Line + 1);
    }
}
#endif
