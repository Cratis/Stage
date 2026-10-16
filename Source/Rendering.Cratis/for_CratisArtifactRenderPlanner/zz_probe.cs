#if DEBUG
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Contracts.Semantics;
using Xunit;
namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;
public class zz_probe
{
    [Fact]
    public async Task probe()
    {
        var dir = Environment.GetEnvironmentVariable("PROBE_DIR");
        if (dir is null) return;
        var name = Environment.GetEnvironmentVariable("PROBE_NAME") ?? "App";
        var compiled = await SemanticModelLoader.LoadFromPathAsync(dir, null, name);
        var plan = CratisRendering.PlanFrom(compiled, new PlanSelection([.. compiled.Model.Application.Modules.Select(m => PlanSelectionEntry.Module(m.Name))]), new CratisPlanOptions(name, name, name));
        var artifacts = Environment.GetEnvironmentVariable("PROBE_ARTIFACTS");
        if (!string.IsNullOrEmpty(artifacts))
        {
            var scaffold = CratisRendering.PlanScaffold(new CratisPlanOptions(name, name, name));
            foreach (var artifact in scaffold.Artifacts.Concat(plan.Artifacts))
            {
                var path = Path.Combine(artifacts, artifact.RelativePath);
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.WriteAllBytes(path, [.. artifact.Bytes]);
            }
        }
        File.WriteAllLines(Environment.GetEnvironmentVariable("PROBE_OUT")!, plan.Diagnostics.Where(d => true).Select(d => $"{d.Severity} {d.Code} {d.Message}"));
    }
}
#endif
