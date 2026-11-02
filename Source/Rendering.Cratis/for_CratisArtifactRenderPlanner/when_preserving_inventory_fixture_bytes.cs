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
        // Re-frozen for Cratis/Stage#120 merged with train 10: authentication artifacts and host wiring on top of the train 10 output.
        ["append refusal"] = "f2da738480ba58919ef5fc7477d9715d21b06e1ce8029ffae7f0f466cc4e80c2",
        ["cascade with trigger"] = "e3289304073f9a9a6163851531c4446e97e57836da2bda222cb9b221bd7930c9",
        ["case table"] = "623dc05f80fd39fe1babc8c85bc29b0f24067f42bb8c6abc66fb15ba8ce560df",
        ["composite keys"] = "fd7e783281a9e7f7d148e248a5c5090317ec758d08000cd17e328cdbe84fa2a5",
        ["denied query"] = "cb36042a87afa01148361b578a34623118765039ad13c56e6f2a06ce81b0b7e6",
        ["folder"] = "59fcd5f056949fe5e92f2cb8e4c46e5712f86b5aec7c9b3382c4aa8dc85d776f",
        ["policy typed contexts"] = "2f2e44c15b53d1ae9b267abe78c6f7d841e0cd248d53bd1d9650f5e62546bf37",
        ["query"] = "eabbebac64b27b0312fd5933d1b5cf9d71a66b7ce4c13ee94a1bdca179357f00",
        ["reaction production"] = "e75c717623deeae584e540cec0d96b8873f87cce2f97baff5a47ad6d89798c84",
        ["redelivery"] = "302eb806a0e1e48be52bf028d74e7a370749eef4dfce02dc27ffea8509617401",
        ["reducer typed contexts"] = "46a58440ccb22c6f72cebd6c962b53b9379b63bffb5edb1a4be34a6fe61d4bec",
        ["relocated"] = "59fcd5f056949fe5e92f2cb8e4c46e5712f86b5aec7c9b3382c4aa8dc85d776f",
        ["reordered"] = "59fcd5f056949fe5e92f2cb8e4c46e5712f86b5aec7c9b3382c4aa8dc85d776f",
        ["scoped projections"] = "7ca702a0a7d2dddd83591ca181189fd583f6816f9149208471880198badd4327",
        ["single"] = "59fcd5f056949fe5e92f2cb8e4c46e5712f86b5aec7c9b3382c4aa8dc85d776f",
        ["strings"] = "ec893996937aa19eb83176683cfa97778420f3ae234d0c5ead5f1dbccbb4b5fa",
        ["translation specification"] = "86bd892deb7659afe0a9bcebb8ac23961dc17e7d80b71b309d7a80e1e67a7a1d",
        ["validation typed contexts"] = "dd22e0f866fe32e560fccd9cef1a0683f5dded5227500ffb0cd189d08434e6c5",
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
