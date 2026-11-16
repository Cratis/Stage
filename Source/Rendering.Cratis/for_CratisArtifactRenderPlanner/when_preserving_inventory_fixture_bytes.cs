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
    // Re-frozen for Cratis/Stage#1061: the shared policy runtime reads the caller's identifier, which moves only the fixtures that render policies.
    // Re-frozen for Cratis/Stage#1061: the shared policy runtime reads the caller's identifier, which moves only the fixtures that render policies.
    // Re-frozen for Cratis/Stage#850: only the policy runtime concept-base comparisons changed.
    // Re-frozen for Cratis/Stage#850: owner-local validation bodies.
    // Recaptured by the owning spec for train 11's merged pins and rendering deliveries.
    // Recaptured for Cratis/Stage#967: Choreography Program.cs hosting and Arc's automatic typed decision reads.
    // Re-frozen for Cratis/Stage#1116: the scaffold registers the provided value resolver, and Screenplay 6.26.0 and Choreography 2.7.0 move the pins.
    // Re-frozen for Cratis/Stage#970: only Program.cs gains the Diagnostics import and defaults event-source id tracing to Omitted.
    static readonly Dictionary<string, string> _digests = new(StringComparer.Ordinal)
    {
        // Re-frozen for Cratis/Stage#120 merged with train 10: authentication artifacts and host wiring on top of the train 10 output.
        // Re-frozen again for Cratis/Stage#850 (denied query, policy typed contexts): only the Policies.cs concept-base comparisons differ from train 10's pin; every other pin equals train 10's after restoring the hashed names.
        ["append refusal"] = "70bcd3a8451ecd10c97c7ffbde8f95a052093fdefc3486ac3cee5afe8c9ffe00",
        ["cascade with trigger"] = "450e7b9c440c51a9eadd4b46acd56840a2f92875d517aaf154c56358890fd0bd",
        ["case table"] = "d852363f8f711b91f1d620389659e4a7b0c8313618b475fcbee13d6b8c112b7e",
        ["composite keys"] = "6982f12b3e8311c3eab43eea7185635fc070ebaac435a357d6da1c7b05fcb979",

        // Re-frozen for Cratis/Stage#1084 (denied query, policy typed contexts): the policy runtime's #pragma lines now render at column 0.
        ["denied query"] = "40d116dbf8f7fa2e373eea94b603f223f6acb506074a7a801b280f22579e3294",
        ["folder"] = "ea8d031a2c39d5acd03f925fddfe02b7f6a9ac7b47b061c330d9f502f8673535",
        ["policy typed contexts"] = "edf910a2398cc26f52beafcb5177e9fb493f6c40f2cc490c212125811deb5dea",
        ["query"] = "64e8e28d211976cc7d794e9d98a8570918c288438976266e4ac5c2f98f38f32c",
        ["reaction production"] = "d376de61358c6a6d0f874bebb68fbcddbb5ab5326c21461a383c9b18a6162d5a",
        ["redelivery"] = "aabe5a9cbfbbfb011ffca27023fc1cc5cd1f3ac303ee447a3da0b5cbf3085281",
        ["reducer typed contexts"] = "745c03955ce9e0e82dd36b157ef04afa414521b26dfee71a9460c231502a1b46",
        ["relocated"] = "ea8d031a2c39d5acd03f925fddfe02b7f6a9ac7b47b061c330d9f502f8673535",
        ["reordered"] = "ea8d031a2c39d5acd03f925fddfe02b7f6a9ac7b47b061c330d9f502f8673535",
        ["scoped projections"] = "84aed15d7a632f94875b56606ef3802af8fa11a268c90cda656e8a3dbb8a9321",
        ["single"] = "ea8d031a2c39d5acd03f925fddfe02b7f6a9ac7b47b061c330d9f502f8673535",
        ["strings"] = "f15a9a1046b0d6c5a15e8ecb6254fee2a4d653975351d71a82a9c211beaafb42",

        // Recaptured for the .NET 11 train: capture state text now freezes the .NET 10 double-to-decimal conversion; reverting that change alone restores 0c3509e0.
        ["translation specification"] = "cbcab49cbfaaa074e6f29a471564eec3b90180fd65f5620845f885875ae7f56b",
        ["validation typed contexts"] = "60b1ce893a6d6956682460a6b2bf3106b9be5492e71535deb6a87cf3ea3de0ea",
    };

    static readonly Dictionary<string, string> _oldDigests = new(StringComparer.Ordinal)
    {
        ["reducer typed contexts"] = "37ddbdc98bfe61e2f0c32294bac2a290caa5b71e5cea2c97a0be304537dbbe1b",
        ["validation typed contexts"] = "6fd39d0c809f1649d3eba980e5ff47af2a929e7ad7eaff5535d5ff0d5d959f20",
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

        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join('\n', lines))));
    }
}
#endif
