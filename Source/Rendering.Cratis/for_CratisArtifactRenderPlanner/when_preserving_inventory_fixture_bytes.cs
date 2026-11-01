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
        ["append refusal"] = "78d5ec8f1baa63a0fb076de02e7803a1f6d9c991d627195ab4a99c1f26088093",
        ["cascade with trigger"] = "db07af34da914f3fd58affdc8f0c448a32640aa625fb04fb3daf0c604dd576fb",
        ["case table"] = "1b3e0ac15a091497e92475c9e64366918cbadd6e47b37e659f50086bd107fdb6",
        ["composite keys"] = "88a84f3331f47e5ea2929a746db464812408844c0e8e332b8446bcda6cd6d8ce",
        ["denied query"] = "234a4f071cab2efc4fc50ff737a682dde6998370729c41d16ed842ad927dd46f",
        ["folder"] = "a78b6abdac06c81869bd9bfa69b8ee797f648f70776d02b05d0623ec133ef03f",
        ["policy typed contexts"] = "528d6ad89dc14c5e400c2a84db3063c658176fc5d4ed4770ee8e468f8477cb32",
        ["query"] = "d95ea5d3926ac57dcd4137856efd7e0bab2ecdbfd2f40c8a9f0ef62c327dc916",
        ["reaction production"] = "ba22da7668d29cf163f92d5cce401b9e84e21fbf571d3d393193b6f8e28c9f34",
        ["redelivery"] = "26f01a0fa7cb373603ea32a037a28ee47e6b1538e94c54bd2ee7e205b1eac29e",
        // Re-frozen for #494: artifacts without explicit authorization defer to host defaults.
        ["reducer typed contexts"] = "9fecdbdc423457aa1bc3495099feedbb8b257776bc63201fe414750d26aedefc",
        ["relocated"] = "a78b6abdac06c81869bd9bfa69b8ee797f648f70776d02b05d0623ec133ef03f",
        ["reordered"] = "a78b6abdac06c81869bd9bfa69b8ee797f648f70776d02b05d0623ec133ef03f",
        ["scoped projections"] = "54756415219d57882746c72297b85286dc7b48ffdd40fce14b21597083521c03",
        ["single"] = "a78b6abdac06c81869bd9bfa69b8ee797f648f70776d02b05d0623ec133ef03f",
        ["strings"] = "b1c1d6c1d68e790fa8b56f27012883adf758524ef7bc97afe3e4388d8a123b51",
        ["translation specification"] = "701faa234978ab5b0e8c1dde2f088d32a7ea082e5f2aa22ae1087401b03e9007",
        ["validation typed contexts"] = "f22a188a7285c06f0c8a1258d17e6c4eba503302b54ef0f53e65753fe9cf83aa",
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
        Assert.Equal(_digests[name], digest);
    }
}
#endif
