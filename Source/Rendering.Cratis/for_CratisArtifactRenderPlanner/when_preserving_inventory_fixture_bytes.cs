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
        ["append refusal"] = "4eea809478af629edd11a2141b393931c8ce54c712aeb158e4517741f4929fbb",
        ["cascade with trigger"] = "99cf23041a0f010449f3ca6cc289b6f1e99b46137f0ffdfd631efa8db1055ebd",
        ["case table"] = "7b432b03d44165cf404c42a4387655702b1ac89e42b02c49627dda66337c2867",
        ["composite keys"] = "f49fb3239668178da155939850a1d3140fb030ad8eba5e7a1cfa78d60e8728b0",
        ["denied query"] = "42dd9efdbc5a559991886803910d16ff57615d59c7cac3dc21843d5cff73e4fb",
        ["folder"] = "0701c170764c388acc3a43012ed4ec738a1313e0663b88f722a7a8e06a3cb8ec",
        ["policy typed contexts"] = "bd03edb6a12d82341eceb3b69fdb3845af3d6cad5bcb641937ef91a4adc6063a",
        ["query"] = "e70254867be99a7ac5032c83e08848352d0b5c0dbcd1bb6dd17aa14862e8b142",
        ["reaction production"] = "6b382ed2e6093546c233a95f1edfc0e53dd55db3b6ec29dc043cdb67ff749efd",
        ["redelivery"] = "f7aa992f8fa7360d20e6647d909cd8988ad2a20aab60f5c464d4f1cc41b5d27b",
        ["reducer typed contexts"] = "d536b2ae8d8146a80eaec0645458e0312e43321f02cde4676043c549f10b69b5",
        ["relocated"] = "0701c170764c388acc3a43012ed4ec738a1313e0663b88f722a7a8e06a3cb8ec",
        ["reordered"] = "0701c170764c388acc3a43012ed4ec738a1313e0663b88f722a7a8e06a3cb8ec",
        ["scoped projections"] = "8fc711d7387a5c25b27821472a1574b6e03ce7f5e5115dcfd60e56e125c9d16d",
        ["single"] = "0701c170764c388acc3a43012ed4ec738a1313e0663b88f722a7a8e06a3cb8ec",
        ["strings"] = "5a0ea3bd492aef0fbf5dab9714ba82f3cbd251cca61080bd9597ba859343cb85",
        ["translation specification"] = "5e94c7234fb95cbc691a9094830011583f05aa2bca2e966e42934a67855e2753",
        ["validation typed contexts"] = "2da453188902c64df4ac75841bbd8462e5004b92d1a62c16bc43655e7aa988fd",
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
