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
    static readonly Dictionary<string, string> _digests = new(StringComparer.Ordinal)
    {
        // Re-frozen for Cratis/Stage#120 merged with train 10: authentication artifacts and host wiring on top of the train 10 output.
        // Re-frozen again for Cratis/Stage#850 (denied query, policy typed contexts): only the Policies.cs concept-base comparisons differ from train 10's pin; every other pin equals train 10's after restoring the hashed names.
        ["append refusal"] = "dfd17e06d3671542d093d647c1107515970df2edf7d4e3f8173d25b72ac146bf",
        ["cascade with trigger"] = "04e736a9cf97cb632bb249c68e83a3f296fc3bb9d1c5cc9950590475f329d317",
        ["case table"] = "eff1bfc1d51a7079b4582ed7039a26e848b95f3324195ae0a68324876896ff1b",
        ["composite keys"] = "af1b26578875184d42bb0509e3eab5f820e3ecf80ca016da7e1dcf911915c6cf",
        ["denied query"] = "aeb6e6fc16de0ed7395284ab3f361b96dfc7b671caf9eba17dca5995888aa2b1",
        ["folder"] = "6996ab793768002e51955d76f14b3c58ee97d282571896c5bec9a8bb2aac3764",
        ["policy typed contexts"] = "32a0647d8fe508b5cc688cf45ebb72c86418e3f3a19785e84f1fd0876327d985",
        ["query"] = "11c4175df1bad828d9d7287b273e4b79ff197cb392357416427a46b71d274155",
        ["reaction production"] = "afea75a256f6f03a22f236b95a028cee2f6fd315aa58e0c9f9c8993326c29f6d",
        ["redelivery"] = "2a6a28bdff8d41c41a4a1946bd8f5765ee148fbb4cb8bb3e1e4bd12b68647f0a",
        ["reducer typed contexts"] = "fe486538ba43a829c035b076ccea976935fa9e18609df8793a70f24ca4f3d18e",
        ["relocated"] = "6996ab793768002e51955d76f14b3c58ee97d282571896c5bec9a8bb2aac3764",
        ["reordered"] = "6996ab793768002e51955d76f14b3c58ee97d282571896c5bec9a8bb2aac3764",
        ["scoped projections"] = "0086d60f22b0bc801b81e5a78e059c76136f964f406a486f7079cbfbd4624da9",
        ["single"] = "6996ab793768002e51955d76f14b3c58ee97d282571896c5bec9a8bb2aac3764",
        ["strings"] = "9e335a6105e41d4cb86a45309c30e105dc7dea0fbfac08d5cb93ad5ee12fbc0e",

        // Recaptured for the .NET 11 train: capture state text now freezes the .NET 10 double-to-decimal conversion; reverting that change alone restores 0c3509e0.
        ["translation specification"] = "fd083ddaf289ab6aab231dbfb95913c3139e6b08f6cb1c8af96da06290e0ab50",
        ["validation typed contexts"] = "b55728602123b752696506bd7a75cdfaaad59a3eed5b79b23c3170aead151bed",
    };

    static readonly Dictionary<string, string> _oldDigests = new(StringComparer.Ordinal)
    {
        ["reducer typed contexts"] = "4d055539a57f591d7d4812ee0e887824c4ff96eb34471fa261defb65b39a1086",
        ["validation typed contexts"] = "8b5810c3c8d51165397976588eca966438ab89e8d570c512afd879b9dbf0c745",
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
