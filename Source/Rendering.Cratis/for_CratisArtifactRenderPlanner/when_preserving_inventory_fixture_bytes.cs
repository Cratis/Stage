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
        ["append refusal"] = "a3b1cac93c0bf89475840795073893d970c3207fcb6b5c00cd3497874d1e77d8",
        ["cascade with trigger"] = "38e77d29119cfdd852cee767efc1fbcc623e4a4009dfc0bd4dfb34827eb01dca",
        ["case table"] = "bd60fe8e8090ccb90f5d26cf9e563d1649f746b0ee4d28bcbc11fd04eef9e40a",
        ["composite keys"] = "fb72c0ffa9b925e47459b89086bfd299f2efbbe3bdb5f4af7fc089db558aa71e",

        // Re-frozen for Cratis/Stage#1084 (denied query, policy typed contexts): the policy runtime's #pragma lines now render at column 0.
        ["denied query"] = "8c68ebff7820433e87cb4a60637281e1989fd9abee728f222f372f950f477785",
        ["folder"] = "9b32bc1025badc390ae5787ff23d344b2a09c3ca8e983d5792dd0a9c2644aba9",
        ["policy typed contexts"] = "4ecaeb32f8fb0652f4fa34ac76c385461d5b204018d2bfa50f9d7fb9c4e1c75b",
        ["query"] = "84cd3f298f2fbc540cde6dc11e2a87d3aa934f519a46cc434ba6f4f525684163",
        ["reaction production"] = "a254b50e506aaa09cca6e75c6eb60b53c0c98395e3b93bb3e5d962be2ef83e5d",
        ["redelivery"] = "85d0e6f3e263512ee782e3ffa30c0d190036053ff41b9dfa280c320113347cb8",
        ["reducer typed contexts"] = "0e12ca0674ff9bae8cb3bfaef6227aeabca069a6c912ee15fd37041c2406de89",
        ["relocated"] = "9b32bc1025badc390ae5787ff23d344b2a09c3ca8e983d5792dd0a9c2644aba9",
        ["reordered"] = "9b32bc1025badc390ae5787ff23d344b2a09c3ca8e983d5792dd0a9c2644aba9",
        ["scoped projections"] = "a81c4dd91e691e87e06c8e0cfea6968259835994fffe4ae4a842638e3257949f",
        ["single"] = "9b32bc1025badc390ae5787ff23d344b2a09c3ca8e983d5792dd0a9c2644aba9",
        ["strings"] = "633b686869ee42fb74449b661ccda425e9b7c5f5dfdf0fca60fc3dc32ee61341",

        // Recaptured for the .NET 11 train: capture state text now freezes the .NET 10 double-to-decimal conversion; reverting that change alone restores 0c3509e0.
        ["translation specification"] = "90b80ca05fb5ff6a78bfe123fba3468933d5bcc25bc227eceb34815b880df21e",
        ["validation typed contexts"] = "f26b426f7d7c07c692febf7ab7b943a983ca3b49ab401f52fc535a87cf1d32da",
    };

    static readonly Dictionary<string, string> _oldDigests = new(StringComparer.Ordinal)
    {
        ["reducer typed contexts"] = "aba6a01eb81801a067481708408be9e96e71c46ff4085ff8e675c7d70f713cbc",
        ["validation typed contexts"] = "1580c7ff7e4787dcab438de4300ffc506afb45cb7d5fcf7e599116756413f803",
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
