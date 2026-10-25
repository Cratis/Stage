// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Emission;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisBackendApplicationScaffold;

public class when_republishing_owned_scaffold_files : Specification
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_apply_the_callers_file_header_to_the_globals(bool header)
    {
        var options = new CratisPlanOptions("Shop", "Shop.Backend", "Acme.Shop") { FileHeader = header ? ["My application"] : [] };
        var plan = CratisRendering.PlanScaffold(options);
        var artifact = plan.Artifacts.Single(candidate => candidate.RelativePath == "GlobalUsings.cs");
        Encoding.UTF8.GetString(artifact.Bytes.AsSpan()).ShouldEqual(
            (header ? "// My application\n\n" : string.Empty) +
            "global using Cratis.Chronicle.Keys;\nglobal using System.Collections.Immutable;\nglobal using System.Reactive.Subjects;\n");
    }

    [Theory]
    [InlineData("GlobalUsings.cs", false)]
    [InlineData("GlobalUsings.cs", true)]
    [InlineData("Program.cs", false)]
    [InlineData("Program.cs", true)]
    [InlineData("Directory.Build.props", false)]
    [InlineData("Directory.Build.props", true)]
    public async Task should_restore_the_planned_bytes_after_an_edit_or_deletion(string relativePath, bool deleted)
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (!File.Exists(Path.Combine(root.FullName, ".git")) && !Directory.Exists(Path.Combine(root.FullName, ".git"))) root = root.Parent!;
        var target = Directory.CreateDirectory(Path.Combine(root.FullName, ".ai-work", "783b", "scaffold-lifecycle", Guid.NewGuid().ToString("N")));
        var path = Path.Combine(target.FullName, relativePath);
        using var output = new StringWriter();
        try
        {
            var plan = CratisRendering.PlanScaffold(new("Shop", "Shop.Backend", "Acme.Shop"));
            plan.Success.ShouldBeTrue();
            var artifact = plan.Artifacts.Single(candidate => candidate.RelativePath == relativePath);
            var file = new RenderedFile(relativePath, Encoding.UTF8.GetString(artifact.Bytes.AsSpan()));
            var publisher = new LocalFileSystemOutput();
            await publisher.Write(file, target, output);
            if (deleted) File.Delete(path);
            else await File.WriteAllTextAsync(path, "// User edit\n");

            var next = CratisRendering.PlanScaffold(new("Shop", "Shop.Backend", "Acme.Shop"));
            next.Success.ShouldBeTrue();
            var replanned = next.Artifacts.Single(candidate => candidate.RelativePath == relativePath);
            await publisher.Write(new(relativePath, Encoding.UTF8.GetString(replanned.Bytes.AsSpan())), target, output);

            (await File.ReadAllBytesAsync(path)).ShouldEqual(artifact.Bytes.ToArray());
        }
        finally
        {
            File.Delete(path);
            target.Delete();
        }
    }
}
