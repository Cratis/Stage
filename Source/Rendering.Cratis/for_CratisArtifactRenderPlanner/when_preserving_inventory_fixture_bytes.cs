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
        ["append refusal"] = "ca05bd7f92862865653a4eafb6925d30688dbb5a90eeefc58e19241bc5a4132b",
        ["cascade with trigger"] = "dcce516c779c267bd7460ed86edaf9ada801279815bdf5e382de7aaabf7d8a24",
        ["case table"] = "a85856d208d532f7f0fea951da4abc42cb88f27a27ccdea54491a04e59749905",
        ["composite keys"] = "88a84f3331f47e5ea2929a746db464812408844c0e8e332b8446bcda6cd6d8ce",
        ["denied query"] = "ce5e38a76b521a1d24cd3aa5a0d33a59641e147c7a8c24081ba09fd953331525",
        ["folder"] = "fbfe7a7f05c3d0cc0dd77ab03a35d56d7cec961a2f2847e3995db9d650c86f82",
        ["policy typed contexts"] = "528d6ad89dc14c5e400c2a84db3063c658176fc5d4ed4770ee8e468f8477cb32",
        ["query"] = "79c15419ef98eec781f2b09c0863fb7f301ea12a83cbbcd0856a98133d74465c",
        ["reaction production"] = "ba22da7668d29cf163f92d5cce401b9e84e21fbf571d3d393193b6f8e28c9f34",
        ["redelivery"] = "ee02a89cd112c05f82772781e72814b6fae42121088db179ac0c513b92786446",
        ["reducer typed contexts"] = "a0cd06b4ee60105295befd8570cc54c4c87ccb6a5032abc9280d6698619a781e",
        ["relocated"] = "fbfe7a7f05c3d0cc0dd77ab03a35d56d7cec961a2f2847e3995db9d650c86f82",
        ["reordered"] = "fbfe7a7f05c3d0cc0dd77ab03a35d56d7cec961a2f2847e3995db9d650c86f82",
        ["scoped projections"] = "c18ad8b3e0701e970e64fdc75a681499c2c994267f4afadc9cc574d4a212d5cb",
        ["single"] = "fbfe7a7f05c3d0cc0dd77ab03a35d56d7cec961a2f2847e3995db9d650c86f82",
        ["strings"] = "b42e9904b27dd4276dd2330e8ba620056ba95d286b1a07087c9ef0d1a72d39c4",
        ["translation specification"] = "99d1840443a3aa73f096c9c46ff3c2b8528348504064645fa61f3ae7b2c73889",
        ["validation typed contexts"] = "1b607de6fc9af9fe526e33a023ac46d34fa9935b9984d7749f6c2826e40e2b76",
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
