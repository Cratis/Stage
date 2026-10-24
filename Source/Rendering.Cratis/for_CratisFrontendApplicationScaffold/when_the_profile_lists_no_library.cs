// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisFrontendApplicationScaffold.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisFrontendApplicationScaffold;

public class when_the_profile_lists_no_library : a_current_frontend_scaffold
{
    [Fact] void should_not_install_prime_packages() => JsonDocument.Parse(Content("package.json")).RootElement.GetProperty("dependencies").EnumerateObject().Any(package => package.Name.Contains("prime", StringComparison.OrdinalIgnoreCase)).ShouldBeFalse();
    [Fact] void should_not_import_prime_css_or_provider_or_license() => Content(".frontend/main.tsx").Contains("prime", StringComparison.OrdinalIgnoreCase).ShouldBeFalse();
    [Fact] void should_not_configure_prime_styling_or_license() => Content(".frontend/vite.config.ts").Contains("prime", StringComparison.OrdinalIgnoreCase).ShouldBeFalse();
    [Fact] void should_render_the_baseline_components() => Content(".frontend/main.tsx").ShouldContain("<App components={cratisComponentsRuntimePackage.components} />");
    [Fact] void should_mount_the_baseline_provider() => Content(".frontend/main.tsx").ShouldContain("<CratisComponentsProvider>");
    [Fact] void should_keep_the_arc_provider() => Content(".frontend/main.tsx").ShouldContain("<Arc>");
    [Fact] void should_keep_the_same_scaffold_paths() => _first.Select(PathOf).SequenceEqual(new Scaffolding.CratisFrontendApplicationScaffold().Create(_request.WithPrimeReact()).Select(PathOf)).ShouldBeTrue();
}
