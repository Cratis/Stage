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
        ["append refusal"] = "48be304d7f9e0939d7ede3f4d664a41be19e4e24dcfea57dfada973a674b2052",
        ["cascade with trigger"] = "5fc8f24f03dd06b4b8ccd0c58fce7d2ba4587ff6a9c451a248c26b92d3b773ee",
        ["case table"] = "c0d5954dcb2cf11d2d879f4deec5bf10a4bec4af5c9983b3d6b7329f573c72b2",
        ["composite keys"] = "10a5213deb52277e340b1c8d0b3f72127ae6c88f353bddf4638cc4360c643b43",
        ["denied query"] = "a67471bcd392fc2cc0ac4b4e4fac7644e43af32d5b33e6412008715f948a61d5",
        ["folder"] = "3daddc08ef1bded62b1118702a5794035a6a2ee2cad015617f58adfbe45efb40",
        ["policy typed contexts"] = "ddb967149f5647e216295a3775b36fd49c05325424964503a6fae5986018c556",
        ["query"] = "44993c07d3c739ed666cebc253d4984e06f6744703d589d27fc3e9abaa19d466",
        ["reaction production"] = "c1f6e7123655a79088956fcfc109820bc1c53f9a4e67ca2d03e7c37070b68a41",
        ["redelivery"] = "d5f3f7756c20e2cfe2f810cb958b70d2db1617b7b824bce603a1f0a9d2304b9f",
        ["reducer typed contexts"] = "90282932c2ed8652f25ce716d377f64b7f7088794583320ff6cac5c04295bffc",
        ["relocated"] = "3daddc08ef1bded62b1118702a5794035a6a2ee2cad015617f58adfbe45efb40",
        ["reordered"] = "3daddc08ef1bded62b1118702a5794035a6a2ee2cad015617f58adfbe45efb40",
        ["scoped projections"] = "461f862905943dcbc0177d2341284f2c5d5aeb7c03a32085c2682460f260774f",
        ["single"] = "3daddc08ef1bded62b1118702a5794035a6a2ee2cad015617f58adfbe45efb40",
        ["strings"] = "26d7421650bc707db1786b26333e7fe1653c0e636a6135674a8c9abd92ddb9e8",
        ["translation specification"] = "6f2df753e163ffefcdd045161d8bbb78159fa55d7273720e27f939005b1c4672",
        ["validation typed contexts"] = "9421fe92b34844f2ff62a69a69ee39313e736220477d4e12fb2fe6e18d0576ef",
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
