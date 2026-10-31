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
    // Re-frozen for #120: every fixture renders the scaffold, which gains the optional Entra ID sign-in files and the Program.cs wiring.
    static readonly Dictionary<string, string> _digests = new(StringComparer.Ordinal)
    {
        ["append refusal"] = "7f3f3c6755c46edb3b9f61c875ddc94aabb4573c7cd3e4d3b30d931ed22081a2",
        ["cascade with trigger"] = "ceae53c2319a8d852485b3962acb8aac6802bd75df4e4051e60f932942684457",
        ["case table"] = "f470fc8e2ce82bd21231dd8bf4220271d88d9b482b24accd2d0be9a59148599c",
        ["composite keys"] = "50bd3d6c6912192329a4274a9d1445b19d1f8ad283e400849a7a3574379c6c62",
        ["denied query"] = "090df479265b37c694d9cd323fb92834711d599186731034d0e33b4a0cb57e9f",
        ["folder"] = "0c1ddf580de35ba24f59a3768d94a3fe8b660770fa1e566fd07ebcb5442b72e0",
        ["policy typed contexts"] = "53834fd0ac2cc70b1bf76f5c84458491c0a242c953a1fbb9c45e3541c882943e",
        ["query"] = "cedb01b5d61bac3d6ba02166a4ec21f8582f3011517e7d758c477b2fdbcd6cfd",
        ["reaction production"] = "a52b8c5d361d842fa4c894a76b55e88b6c96ec20e0eeb9930f985070ec6688bc",
        ["redelivery"] = "24d5fcb2b60d726cd3dccf68548615ec57b15bdf32251897b1f90e9ed8b49462",
        ["reducer typed contexts"] = "84f3e60522814feb0664dffe7a6f5a43004b4db2237ab37b4116398043f8c930",
        ["relocated"] = "0c1ddf580de35ba24f59a3768d94a3fe8b660770fa1e566fd07ebcb5442b72e0",
        ["reordered"] = "0c1ddf580de35ba24f59a3768d94a3fe8b660770fa1e566fd07ebcb5442b72e0",
        ["scoped projections"] = "160ef9ad151f0afed57ee72d02fc43b88b985e5e8e82f11c45449fe1bc500021",
        ["single"] = "0c1ddf580de35ba24f59a3768d94a3fe8b660770fa1e566fd07ebcb5442b72e0",
        ["strings"] = "2a3921e50e411655a3d855c1b87ef38b47401cbebd7095e8b2b72fb8433a6000",
        ["translation specification"] = "903166e56c85c8eb1497b42ee1822371558bb413f20298253b04d1784224fab5",
        ["validation typed contexts"] = "08d4d4e51a670b39725d1f144c8b17c47ad87528a03d0a8db2654d68d2a19793",
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
