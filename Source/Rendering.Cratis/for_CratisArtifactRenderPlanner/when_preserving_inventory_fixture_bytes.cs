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
        ["append refusal"] = "40162a348403e9d792101c275306a96c2834c709faca90f097769ed5321b9ad7",
        ["cascade with trigger"] = "81d41901e41755eed0c607edc7177ee69f0ec22cfe494b583318058a612d3dda",
        ["case table"] = "db27485845dbbc9febff590eddc648c7f78a89772afb1a789fbb61ce18654137",
        ["composite keys"] = "f49fb3239668178da155939850a1d3140fb030ad8eba5e7a1cfa78d60e8728b0",
        ["denied query"] = "98c4b61690f46793a8af628b0fb3ee9b52a8baf1583a356f8340763fe928bd4d",
        ["folder"] = "472e7680397cc5554d3582e436519098dcd83fc6619281cf1d3674e399aa782b",
        ["policy typed contexts"] = "bd03edb6a12d82341eceb3b69fdb3845af3d6cad5bcb641937ef91a4adc6063a",
        ["query"] = "2b177e776044740057a19ddc3270b94dd9bd7fbc264fbc751160105e7548657f",
        ["reaction production"] = "6b382ed2e6093546c233a95f1edfc0e53dd55db3b6ec29dc043cdb67ff749efd",
        ["redelivery"] = "250bea136b3f4dceb4e0378f345d64e6e9f708519ecc5c2b8a5f5e42d4091392",
        ["reducer typed contexts"] = "d25b6c2d8f5ad0b80973a7e3d89d082d339e868508912f256c97f2823b6c6adc",
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
