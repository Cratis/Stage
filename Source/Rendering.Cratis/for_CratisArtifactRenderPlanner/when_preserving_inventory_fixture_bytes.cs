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
        ["redelivery"] = "250bea136b3f4dceb4e0378f345d64e6e9f708519ecc5c2b8a5f5e42d4091392",
        ["reducer typed contexts"] = "32e2f14dca8bb900eddd8a2a2791b5577209f72af09877977b8ac3ef08e4c2bd",
        ["relocated"] = "472e7680397cc5554d3582e436519098dcd83fc6619281cf1d3674e399aa782b",
        ["reordered"] = "472e7680397cc5554d3582e436519098dcd83fc6619281cf1d3674e399aa782b",
        ["scoped projections"] = "dd1d283e2acd2dd3aaf27c4642045497091d5f7cadaba85c2f246ddc5c2bd485",
        ["single"] = "472e7680397cc5554d3582e436519098dcd83fc6619281cf1d3674e399aa782b",
        ["strings"] = "02a6a179fd5a7c2b9517be88980f361961ce0729c20b8691197f716dffa779cb",
        ["translation specification"] = "1de4e0c30247eb94fdbd9c3b1c0fa729fbaed69711275787c9d7855be068496e",
        ["validation typed contexts"] = "2aac89971f306eda14e3eed70316276b3864bb26812f1027f83f077c4b1504d3",
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
