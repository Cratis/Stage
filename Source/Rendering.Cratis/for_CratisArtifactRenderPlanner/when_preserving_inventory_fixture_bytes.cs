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
        ["append refusal"] = "1aa07959324081b17bc3696f4634061178e1b72f5956b674ea493d313b9626c2",
        ["cascade with trigger"] = "a8877151cc4ac0451688a89395e174ac61a53e47586be09845653052f7b1a8e4",
        ["case table"] = "8ecb81e771ff44e5ca2a32f4fc74c34852499ec8c1faa7a3ceeb88e336282507",
        ["composite keys"] = "e4b1ee2d1761be5c87c60ad06eb8f6b2f51e8ad949e673f2f36133f36a92291c",
        ["denied query"] = "104fbef6ed63be6326d41b207642f5ddf37b7db8abd84339a5ba0496c53c87f5",
        ["folder"] = "1e1f9b4c9e2ad20712ee7445b3693dbd977f370d2b806d81894b8bad25aa9c86",
        ["policy typed contexts"] = "92312ff012dd771d3bbf9b05a8a329a480ac9e71ce4012daeb637c89132278b4",
        ["query"] = "0acf113719c869297e1d8cec28030cff428967be80ec1d2eda959122552681f6",
        ["reaction production"] = "6304eacefb1c832c70cb5de985957e5e3ae1ea05179e97852643698a7eb6b455",
        ["redelivery"] = "196e330aa1f1f41d97b70748a37075e25af5a7db2bd50c475e572b25f80c4429",
        ["reducer typed contexts"] = "623e1b941bb247b812a35ce67f7cdb50b4777e869683bf64bb744778aeb44342",
        ["relocated"] = "1e1f9b4c9e2ad20712ee7445b3693dbd977f370d2b806d81894b8bad25aa9c86",
        ["reordered"] = "1e1f9b4c9e2ad20712ee7445b3693dbd977f370d2b806d81894b8bad25aa9c86",
        ["scoped projections"] = "5004f9ca040c69b5900702a066e8483de4f83e0399a847eb781b37d5e729450c",
        ["single"] = "1e1f9b4c9e2ad20712ee7445b3693dbd977f370d2b806d81894b8bad25aa9c86",
        ["strings"] = "3f465801ce5b2f104a8c00408ab621963fae6fe89e0db7d7fa94dc916ac398d2",
        ["translation specification"] = "b00e0c86411e540aa7c8adb286efd81197e6c6b970d811c9259f874054d495d4",
        ["validation typed contexts"] = "e277dc52789c9e697ec67ffaff297c4225b75f844ab51fa95e25844b550925a6",
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
