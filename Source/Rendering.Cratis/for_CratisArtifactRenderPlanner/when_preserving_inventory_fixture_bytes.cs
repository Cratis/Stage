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
    // Re-frozen for Cratis/Stage#850: only the policy runtime concept-base comparisons changed.
    // Re-frozen for Cratis/Stage#850: owner-local validation bodies
    static readonly Dictionary<string, string> _digests = new(StringComparer.Ordinal)
    {
        // Re-frozen for Cratis/Stage#120 merged with train 10: authentication artifacts and host wiring on top of the train 10 output.
        // Re-frozen again for Cratis/Stage#850 (denied query, policy typed contexts): only the Policies.cs concept-base comparisons differ from train 10's pin; every other pin equals train 10's after restoring the hashed names.
        ["append refusal"] = "f2da738480ba58919ef5fc7477d9715d21b06e1ce8029ffae7f0f466cc4e80c2",
        ["cascade with trigger"] = "e3289304073f9a9a6163851531c4446e97e57836da2bda222cb9b221bd7930c9",
        ["case table"] = "623dc05f80fd39fe1babc8c85bc29b0f24067f42bb8c6abc66fb15ba8ce560df",
        ["composite keys"] = "fd7e783281a9e7f7d148e248a5c5090317ec758d08000cd17e328cdbe84fa2a5",
        ["denied query"] = "c0ac4f491fabd64efe9444b681fdcec11c2489db427d04e34e312bc8cb1986ab",
        ["folder"] = "59fcd5f056949fe5e92f2cb8e4c46e5712f86b5aec7c9b3382c4aa8dc85d776f",
        ["policy typed contexts"] = "c611e89e4fb3e0ab28c5cc95a852a53412e4b9509ea5287f351f9ffcb1223cb5",
        ["query"] = "eabbebac64b27b0312fd5933d1b5cf9d71a66b7ce4c13ee94a1bdca179357f00",
        ["reaction production"] = "e75c717623deeae584e540cec0d96b8873f87cce2f97baff5a47ad6d89798c84",
        ["redelivery"] = "302eb806a0e1e48be52bf028d74e7a370749eef4dfce02dc27ffea8509617401",
        ["reducer typed contexts"] = "f4f8f4e105c3994b87e855d1f61a96579e11182c31915b3099c243f009a7fa3c",
        ["relocated"] = "59fcd5f056949fe5e92f2cb8e4c46e5712f86b5aec7c9b3382c4aa8dc85d776f",
        ["reordered"] = "59fcd5f056949fe5e92f2cb8e4c46e5712f86b5aec7c9b3382c4aa8dc85d776f",
        ["scoped projections"] = "7ca702a0a7d2dddd83591ca181189fd583f6816f9149208471880198badd4327",
        ["single"] = "59fcd5f056949fe5e92f2cb8e4c46e5712f86b5aec7c9b3382c4aa8dc85d776f",
        ["strings"] = "ec893996937aa19eb83176683cfa97778420f3ae234d0c5ead5f1dbccbb4b5fa",
        ["translation specification"] = "86bd892deb7659afe0a9bcebb8ac23961dc17e7d80b71b309d7a80e1e67a7a1d",
        ["validation typed contexts"] = "9364f551494000760b21c7d08115e096c8dd9715797ead496ec3d5c98cbdfebe",
    };

    static readonly Dictionary<string, string> _oldDigests = new(StringComparer.Ordinal)
    {
        ["reducer typed contexts"] = "46a58440ccb22c6f72cebd6c962b53b9379b63bffb5edb1a4be34a6fe61d4bec",
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
        var (_, request, options) = inventory_fixtures.All().Single(fixture => fixture.Name == name);
        var plan = new CratisArtifactRenderPlanner().Plan(request);
        Assert.True(plan.Success, string.Join('\n', plan.Diagnostics));

        // Cratis/Stage#850 made typed-context names readable; restoring the hashed spelling proves the names were the only change.
        var context = new Semantics.SemanticApplicationContext(request, options);
        if (_oldDigests.TryGetValue(name, out var oldDigest))
        {
            var inverse = SharedValidationBodies.Restore(plan.Artifacts, context);
            Assert.Equal(oldDigest, Digest(HashedHelperNames.Restore(inverse, context)));
        }
        Assert.Equal(_digests[name], Digest(HashedHelperNames.Restore(plan.Artifacts, context)));
    }

    static string Digest(IEnumerable<Contracts.Rendering.PlannedArtifact> artifacts)
    {
        var lines = artifacts.Where(artifact => artifact.RelativePath.EndsWith(".cs", StringComparison.Ordinal))
            .OrderBy(artifact => artifact.RelativePath, StringComparer.Ordinal)
            .Select(artifact => $"{artifact.RelativePath} {artifact.Sha256}");
        var digest = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join('\n', lines))));
        return digest;
    }
}
#endif
