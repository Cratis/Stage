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
    // Re-frozen for Cratis/Stage#850: owner-local validation bodies.
    // Recaptured by the owning spec for train 11's merged pins and rendering deliveries.
    static readonly Dictionary<string, string> _digests = new(StringComparer.Ordinal)
    {
        // Re-frozen for Cratis/Stage#120 merged with train 10: authentication artifacts and host wiring on top of the train 10 output.
        // Re-frozen again for Cratis/Stage#850 (denied query, policy typed contexts): only the Policies.cs concept-base comparisons differ from train 10's pin; every other pin equals train 10's after restoring the hashed names.
        ["append refusal"] = "a22415a6ac0cba579b22d7981822fa9089d259f0a7ce452e2472d4e749e50350",
        ["cascade with trigger"] = "62f05f08ca11aac19824d16c1fe2c644b457e0cab91425a57bbc02c71cdb0d7b",
        ["case table"] = "6c905812c89480e4b160f5f7d2b88287316b18c0e430c12296c2c4bb43879810",
        ["composite keys"] = "d09530f3c863ddacf4d8ba571f85529207e04170e2900b201b23a424a211a8d9",
        ["denied query"] = "057ceee5b315ed5f4999d059c641df84ad7e7d1a0001e8ec19db8bf291cf7198",
        ["folder"] = "bf29b7c91621bc4e1e1f840605a0de7452295f05201f7ae95c481b96d852ae47",
        ["policy typed contexts"] = "e3a1c7f6a06ee335142d6f4ee65922d5edd822b8739bfc3827cc7aa4626065b8",
        ["query"] = "9994fbe1797a1edabed9ce62742b2696aa30f0494391d30c97427052c731f3cd",
        ["reaction production"] = "6e358f8b949fd8aeb3acf3c6aec8a0ba2181cfb1ddfa4d3e70b60d09faefae29",
        ["redelivery"] = "53768fff851033b63bc8e85d32cd25096ae7c02820428ffa25fcab3a57cd6509",
        ["reducer typed contexts"] = "b0a1f53e43127fb95b801768981bd19adab1ac54c31a2269affb86fa321e2a9d",
        ["relocated"] = "bf29b7c91621bc4e1e1f840605a0de7452295f05201f7ae95c481b96d852ae47",
        ["reordered"] = "bf29b7c91621bc4e1e1f840605a0de7452295f05201f7ae95c481b96d852ae47",
        ["scoped projections"] = "9fbe7aada753d1dde7d45e90394a624b29d014aef69ea6a295b530fb94ac99e7",
        ["single"] = "bf29b7c91621bc4e1e1f840605a0de7452295f05201f7ae95c481b96d852ae47",
        ["strings"] = "bf43eeb3a0f064c7ff43ed6f32f6c7b2fa5f15504d8ef080463edb5a75f50d02",
        ["translation specification"] = "722b6ed8cf313a8b63d4cc2f9aa03e2371886ae36147690b89717ea4b609c517",
        ["validation typed contexts"] = "1f017cf987cd63fdfbe926880fe3684cfd4faf4f0810b768eb3c98bb4fa362d7",
    };

    static readonly Dictionary<string, string> _oldDigests = new(StringComparer.Ordinal)
    {
        ["reducer typed contexts"] = "45d80db27e1610c60427e059fa299942066a521be9054064e353ad1aa8afcf3d",
        ["validation typed contexts"] = "bc095521dd2d2d2f69eaec662250b4dae937aafc01a2b524d90cc3136470cbe5",
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
