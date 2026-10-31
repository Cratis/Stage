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
        ["append refusal"] = "e96493c4cce9486c5cec06142a8b397233847572245bb17f8d9d11f4a68782b7",
        ["cascade with trigger"] = "ae5ce72a1dcbdb551d8d5458c8f626668d1084e9edc339b01afb84ef48f8698a",
        ["case table"] = "d78612e0af251d6b56d3f8c05344a625335ddc6b092dc795dd0ceb0c045f3fc6",
        ["composite keys"] = "f49fb3239668178da155939850a1d3140fb030ad8eba5e7a1cfa78d60e8728b0",
        ["denied query"] = "a18604211856bb3a68ca46fb01836e7812e0c781a070ef8af86604af62c339ac",
        ["folder"] = "45edad68ceee6d0f755e62771287a9e49b50179d81879922ddaab8f94114a308",
        ["policy typed contexts"] = "bd03edb6a12d82341eceb3b69fdb3845af3d6cad5bcb641937ef91a4adc6063a",
        ["query"] = "9abede2f083c0c0f2d20fecbfb841763fb516881128af26520912437a6fa1e11",
        ["reaction production"] = "6b382ed2e6093546c233a95f1edfc0e53dd55db3b6ec29dc043cdb67ff749efd",
        ["redelivery"] = "84c0e06e248f59586261abb1aac3338b389ba8f7ef0694409a303769dbc17c2f",
        ["reducer typed contexts"] = "32e2f14dca8bb900eddd8a2a2791b5577209f72af09877977b8ac3ef08e4c2bd",
        ["relocated"] = "45edad68ceee6d0f755e62771287a9e49b50179d81879922ddaab8f94114a308",
        ["reordered"] = "45edad68ceee6d0f755e62771287a9e49b50179d81879922ddaab8f94114a308",
        ["scoped projections"] = "f509232967f361c97a423493ba6bb7f80f7c963602a44c65654095147ec1881f",
        ["single"] = "45edad68ceee6d0f755e62771287a9e49b50179d81879922ddaab8f94114a308",
        ["strings"] = "d19b172eaf89cd3bd270aff2448513a8f640ff812cb1e634806274068c9f1b88",
        ["translation specification"] = "06e1dcb98b836d6a27fc3160704e748ca8bd68a6031ad213bb3b86a1b0de26fe",
        ["validation typed contexts"] = "f7194d3bb4e74d1065627468acdfca54b28e02454f707caaab8aaa83af941d8c",
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
