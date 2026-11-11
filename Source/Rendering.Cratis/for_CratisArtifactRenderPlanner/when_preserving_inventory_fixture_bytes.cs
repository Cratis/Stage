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
        ["append refusal"] = "1379302584083e03028878abc71acc2ffa62eac4aeb74b57502b57d5657c1cfd",
        ["cascade with trigger"] = "bf0ee35d52288dbaaa630d07eaa6fc5efe05734885ca17fabfe8877db30a74e6",
        ["case table"] = "92501aeb7edf5953fe9ed8d804dbe04d3561098a61a79b9ab4ea71a4d63196cb",
        ["composite keys"] = "3d41b1682f085b340c5fd1ea4418c295a8edb0790326621aa141be240e811103",
        ["denied query"] = "d0318a516265f1c1304b768fbe783770b5fa2b459a9bace68ff4bc682489b6aa",
        ["folder"] = "cebd157e8962bad41d07bf28e639e7d320eb36c194ec520e0b1d040fb722610a",
        ["policy typed contexts"] = "f164eb300c6aa9477bc642fb8624cd853ff4857b8791156f2404302e12f8d68e",
        ["query"] = "1917cc5c7594ad360c1810d91f9c093f76fde68f4194c54df1ce32405a41236e",
        ["reaction production"] = "f905251195cd4eb0f9dc76270354240e88dddd925249ec71299e63e04d5b78a1",
        ["redelivery"] = "aa06faa1305615356b4b9cf2f33cca62afc96932812e1ca0c0e3b57b9e0961d7",
        ["reducer typed contexts"] = "fa64eb940905c2a3bb0a78043128110b89c0c8d9770f8f410f72694cb6258c5a",
        ["relocated"] = "cebd157e8962bad41d07bf28e639e7d320eb36c194ec520e0b1d040fb722610a",
        ["reordered"] = "cebd157e8962bad41d07bf28e639e7d320eb36c194ec520e0b1d040fb722610a",
        ["scoped projections"] = "b035db35f77822863824f9b1d20e9f4a64b060be9acd92d694c69a69983decfc",
        ["single"] = "cebd157e8962bad41d07bf28e639e7d320eb36c194ec520e0b1d040fb722610a",
        ["strings"] = "5d6d806d8690c7628e63adc80604c6e935faf3d7bf1b1d0264ab0312f9878838",

        // Recaptured for the .NET 11 train: capture state text now freezes the .NET 10 double-to-decimal conversion; reverting that change alone restores 0c3509e0.
        ["translation specification"] = "0eab573aade4618c8b47a236ff7b96aa6bc9f2d26cea1b54fb500e1673a474d8",
        ["validation typed contexts"] = "cf23b530db1f7081dca03eb8aa70d4e58e9ddbd7b38ed2f6ec86feb62f7b39d6",
    };

    static readonly Dictionary<string, string> _oldDigests = new(StringComparer.Ordinal)
    {
        ["reducer typed contexts"] = "9364364a84f347e053d0dde376786193c5b80a4112c2f84a769597e070a03059",
        ["validation typed contexts"] = "6fff8ac7640650a12e148f353189ca9bdc19bbfc645c7cab2ef4fcc53dab32ff",
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
