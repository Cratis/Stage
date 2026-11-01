// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Kits;

namespace Cratis.Stage.Rendering.Templates.for_TemplatePreparer.given;

public class a_kit : Specification
{
    protected static LoadedKit Kit(string text = "Hello {{ model.name }}", string path = "file.txt", string? when = null, string? forEach = null)
    {
        var manifest = new KitManifest(new("stage-target", 1, 0), new("test", "1.0.0"), [], [new("templates", 1, 0)], [new KitOutput("main", "main.scriban", path) { When = when, ForEach = forEach }], [], [], [], []);
        return new(manifest, new KitFiles([new KeyValuePair<string, ImmutableArray<byte>>("main.scriban", [.. Encoding.UTF8.GetBytes(text)])]), [], "test");
    }
}
