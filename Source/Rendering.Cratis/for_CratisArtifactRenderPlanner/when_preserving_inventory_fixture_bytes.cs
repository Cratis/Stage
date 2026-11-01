// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Security.Cryptography;
using System.Text;
using Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner;

/// <summary>
/// Pins the rendered C# of every generated-type inventory fixture, so completing the inventory (Cratis/Stage#858)
/// provably moves no byte: the inventory is consumed only by C# import resolution.
/// </summary>
public class when_preserving_inventory_fixture_bytes
{
    static readonly Dictionary<string, string> _digests = new(StringComparer.Ordinal)
    {
        ["append refusal"] = "REGENERATE",
        ["cascade with trigger"] = "REGENERATE",
        ["case table"] = "REGENERATE",
        ["composite keys"] = "REGENERATE",
        ["denied query"] = "REGENERATE",
        ["folder"] = "REGENERATE",
        ["policy typed contexts"] = "REGENERATE",
        ["query"] = "REGENERATE",
        ["reaction production"] = "REGENERATE",
        ["redelivery"] = "REGENERATE",
        // Re-frozen for #494: artifacts without explicit authorization defer to host defaults.
        ["reducer typed contexts"] = "REGENERATE",
        ["relocated"] = "REGENERATE",
        ["reordered"] = "REGENERATE",
        ["scoped projections"] = "REGENERATE",
        ["single"] = "REGENERATE",
        ["strings"] = "REGENERATE",
        ["translation specification"] = "REGENERATE",
        ["validation typed contexts"] = "REGENERATE",
    };

    public static TheoryData<string> Fixtures
    {
        get
        {
            var data = new TheoryData<string>();
            foreach (var fixture in inventory_fixtures.All())
            {
                data.Add(fixture.Name);
            }

            return data;
        }
    }

    [Theory]
    [MemberData(nameof(Fixtures))]
    public void should_preserve_the_rendered_csharp(string name)
    {
        var (_, request, _) = inventory_fixtures.All().Single(fixture => fixture.Name == name);
        var plan = new CratisArtifactRenderPlanner().Plan(request);
        Assert.True(plan.Success, string.Join('\n', plan.Diagnostics));
        var lines = plan.Artifacts.Where(artifact => artifact.RelativePath.EndsWith(".cs", StringComparison.Ordinal))
            .OrderBy(artifact => artifact.RelativePath, StringComparer.Ordinal)
            .Select(artifact => $"{artifact.RelativePath} {artifact.Sha256}");
        var digest = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join('\n', lines))));
        Assert.True(_digests[name] == digest, $"PIN {name}: {digest}");
    }
}
#endif
