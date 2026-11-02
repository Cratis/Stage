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
        ["append refusal"] = "4f687d4f843f928dc2d91c97ef872758b1ff543b9e2af026190e7457251c1fcb",
        ["cascade with trigger"] = "71935682ec1409beb3960440c6d16149b46392bd20131fc46551a373d068bebc",
        ["case table"] = "0ed6c3bac91b03b2f5730f55cfa7410b4bdf1055e44517bf82ae92032e77de9f",
        ["composite keys"] = "98dd134bef95b9c449114a3b03e9253cdd1fa1bfe5259d3ed224d87753572902",
        ["denied query"] = "e52b5dbe2131994311cc43c180fb4d915ec8a9634287d4fe7d53de271784aa88",
        ["folder"] = "b777fad0d39a588c848f15eec7b1f7457f89d5e8b5aab904ff33d1bb0d7adf2f",
        ["policy typed contexts"] = "2c5a4f7e82aae06def85ebc011404e272184986af24efff290fdcbc4c2d0b288",
        ["query"] = "c404637a718fd37425a33586b46169c503eead593db42c3d91254bb202efc65c",
        ["reaction production"] = "8757de1c8e448d8fd11531adba12b201ad8ba4102e631a2293dfdfd118638672",
        ["redelivery"] = "99892953aec640a9f7b57b59fc0f14f7ce7bf1c0708ee3759b7a9ad0733ad91b",
        ["reducer typed contexts"] = "d6228f62cb676be70a71f9326177948a8abf71e766b3894eef624c6dddd52c7d",
        ["relocated"] = "b777fad0d39a588c848f15eec7b1f7457f89d5e8b5aab904ff33d1bb0d7adf2f",
        ["reordered"] = "b777fad0d39a588c848f15eec7b1f7457f89d5e8b5aab904ff33d1bb0d7adf2f",
        ["scoped projections"] = "dd8c99211cc6e1256702f3187d8c5267e607baa460e0dbe6abf3eacaee531133",
        ["single"] = "b777fad0d39a588c848f15eec7b1f7457f89d5e8b5aab904ff33d1bb0d7adf2f",
        ["strings"] = "1149128548f2b04da6daedd9d76c3d5502a4492ec76b268a4bc7b1754778dc8b",
        ["translation specification"] = "dbe95a036217761c77c1bc51aae5a511bc1c7f546198f3d06af16bc1943a8e16",
        ["validation typed contexts"] = "67a37354f7918a10c02f41f749f7a415f446be042ceb069908c4bc95be9a4e61",
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
