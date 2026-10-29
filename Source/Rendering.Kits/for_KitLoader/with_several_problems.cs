// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Kits.for_KitLoader;

public class with_several_problems : Specification
{
    KitLoadResult _result = null!;

    void Because()
    {
        var files = given.kit.Files(given.kit.Manifest.Replace("java@1.0.0", "java@2.0.0", StringComparison.Ordinal));
        files.Remove("templates/main.sbn");
        files.Add("z", []);
        files.Add("a", []);
        _result = KitLoader.Load(new InMemoryKitSource(files), given.kit.Capabilities);
    }

    [Fact] void should_not_return_a_partial_kit() => _result.Kit.ShouldBeNull();
    [Fact] void should_report_all_problems_in_code_then_path_order() => _result.Diagnostics.Select(diagnostic => (diagnostic.Code, diagnostic.Path)).ShouldEqual([(KitCodes.MissingLanguagePack, "$.languagePacks"), (KitCodes.MissingFile, "templates/main.sbn"), (KitCodes.UndeclaredFile, "a"), (KitCodes.UndeclaredFile, "z")]);
}
