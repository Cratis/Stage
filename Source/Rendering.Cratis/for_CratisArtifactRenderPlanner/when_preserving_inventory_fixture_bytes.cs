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
    static readonly Dictionary<string, string> _digests = new(StringComparer.Ordinal)
    {
        // Re-frozen for Cratis/Stage#120 merged with train 10: authentication artifacts and host wiring on top of the train 10 output.
        // Re-frozen again for Cratis/Stage#850 (denied query, policy typed contexts): only the Policies.cs concept-base comparisons differ from train 10's pin; every other pin equals train 10's after restoring the hashed names.
        ["append refusal"] = "c53d525844f4438cb764687f5aa01fb9d2ce34f08232f012af0d2a88b4b0fe0b",
        ["cascade with trigger"] = "5a5eb142e53eb3a1a6cd66db740e0f8f08f1aa7df39f283fc1a8e845066f84b1",
        ["case table"] = "05db5a094f8eed38c5449c891f19a8ace829e1fa681ae303e3de163c4aaa5019",
        ["composite keys"] = "5b596e37dbfa474c0c558dd654c5db4c247fa70fba2caa1105df4665cc55fdec",

        // Re-frozen for Cratis/Stage#1084 (denied query, policy typed contexts): the policy runtime's #pragma lines now render at column 0.
        ["denied query"] = "97622cd3bf6c42ffef84d0459d1a5edd30833fffca4e526dd94e7d01384db097",
        ["folder"] = "508320fd67cefd62ef46abf6e6ad3e69d4a86e0a4778a1d8dc8f28c1834045cc",
        ["policy typed contexts"] = "97ba0e4ba08573ca4e2adf83bacd1a0fd282f555b56ad0980d4ed2d234355c07",
        ["query"] = "8cf138ee589a05a823c06c458f56fd0e5dce806cb9290dc6ecf18d742097cb90",
        ["reaction production"] = "0dced85c36fcc3763e475258ef8d344841e164a0988d3d8d1c5be87f30db5f8b",
        ["redelivery"] = "78a1c1ba9bd7fc4794dd855a2644e579531b9f368c717888ea5108e9b87fc769",
        ["reducer typed contexts"] = "0f799c490a1b03223bb0f56677ff14777e699afb83433cecca7dbf22d41ce63d",
        ["relocated"] = "508320fd67cefd62ef46abf6e6ad3e69d4a86e0a4778a1d8dc8f28c1834045cc",
        ["reordered"] = "508320fd67cefd62ef46abf6e6ad3e69d4a86e0a4778a1d8dc8f28c1834045cc",
        ["scoped projections"] = "1c8f793eb8bac99c10b0fe4ed6ddc846692c27363c01aebf0b9a7730f43136a4",
        ["single"] = "508320fd67cefd62ef46abf6e6ad3e69d4a86e0a4778a1d8dc8f28c1834045cc",
        ["strings"] = "62e290a21d266a3fb311ba9dd5f11e46e8bfb33a1eaa55e5fb921baa918d8d97",

        // Recaptured for the .NET 11 train: capture state text now freezes the .NET 10 double-to-decimal conversion; reverting that change alone restores 0c3509e0.
        ["translation specification"] = "fa4cd2af09837610161a4697bb64a5335978213c5b63585eaa1fe8ceb9fab99c",
        ["validation typed contexts"] = "f91e20a5b0723a50c87238e9a0d44594a10e724af749da2440e8b65c2017124c",
    };

    static readonly Dictionary<string, string> _oldDigests = new(StringComparer.Ordinal)
    {
        ["reducer typed contexts"] = "4bde8f92e298122246b1fcaaa95740682380868c13c2e81601dee0f2bda6ae0e",
        ["validation typed contexts"] = "fb920583751065f1b4909a599e3515c24d02137640768d545366130ccf08eb52",
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
