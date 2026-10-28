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
        ["append refusal"] = "96964e86f5ef9dc605df68f2736260cdb704dc87dd384005d4106898f13ba3b9",
        ["cascade with trigger"] = "81d41901e41755eed0c607edc7177ee69f0ec22cfe494b583318058a612d3dda",
        ["case table"] = "db27485845dbbc9febff590eddc648c7f78a89772afb1a789fbb61ce18654137",
        ["composite keys"] = "f49fb3239668178da155939850a1d3140fb030ad8eba5e7a1cfa78d60e8728b0",
        ["denied query"] = "98c4b61690f46793a8af628b0fb3ee9b52a8baf1583a356f8340763fe928bd4d",
        ["folder"] = "99003ea59143a1e3804851a4e4dd793041c8925ef8fdcb389dac05623102ff1e",
        ["policy typed contexts"] = "b10723da8b3205eae18d0ab8092a1071076e4f81f3ee9f19f1877dd85c1f14da",
        ["query"] = "2b177e776044740057a19ddc3270b94dd9bd7fbc264fbc751160105e7548657f",
        ["reaction production"] = "6b382ed2e6093546c233a95f1edfc0e53dd55db3b6ec29dc043cdb67ff749efd",
        ["redelivery"] = "250bea136b3f4dceb4e0378f345d64e6e9f708519ecc5c2b8a5f5e42d4091392",
        ["reducer typed contexts"] = "93e40d3c742c7cb1b36baded80512e63150192e4b16a69cdab8d08f28842dad7",
        ["relocated"] = "99003ea59143a1e3804851a4e4dd793041c8925ef8fdcb389dac05623102ff1e",
        ["reordered"] = "99003ea59143a1e3804851a4e4dd793041c8925ef8fdcb389dac05623102ff1e",
        ["scoped projections"] = "dd1d283e2acd2dd3aaf27c4642045497091d5f7cadaba85c2f246ddc5c2bd485",
        ["single"] = "99003ea59143a1e3804851a4e4dd793041c8925ef8fdcb389dac05623102ff1e",
        ["strings"] = "0dc0272fc6d214c0f162ed34f0ab6e94cfeb85f5f0daab1e04a4bed149683800",
        ["translation specification"] = "de52b358cf957c23f63097ddbe9ca0406cc08c3cdbc3c16f20d1dec894ad57d1",
        ["validation typed contexts"] = "8248b722fac18810632e26b7307338ec3e00edc707c70c6d126c324fb4ff1625",
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
