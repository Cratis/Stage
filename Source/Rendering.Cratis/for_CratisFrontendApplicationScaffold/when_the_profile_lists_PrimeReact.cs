// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisFrontendApplicationScaffold.given;
using Cratis.Stage.Rendering.Cratis.Scaffolding;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisFrontendApplicationScaffold;

public class when_the_profile_lists_PrimeReact : a_current_frontend_scaffold
{
    void Because() => _first = new CratisFrontendApplicationScaffold().Create(_request.WithPrimeReact());

    [Fact] void should_mount_the_licensed_themed_provider() => Content(".frontend/main.tsx").ShouldContain("<PrimeReactProvider license={primeUiLicense} theme={stageTheme}>");
    [Fact] void should_keep_the_build_time_license() => Content(".frontend/main.tsx").ShouldContain("const primeUiLicense = import.meta.env.STAGE_PRIMEUI_LICENSE || '';");
    [Fact] void should_import_the_theme_from_the_opt_in_entry() => Content(".frontend/main.tsx").ShouldContain("import { stageTheme } from '@cratis/stage.runtime/primereact';");
    [Fact] void should_not_import_the_theme_from_the_root() => Content(".frontend/main.tsx").ShouldNotContain("    stageTheme,\n");
    [Fact] void should_keep_the_prime_theme_css() => Content(".frontend/main.tsx").ShouldContain("import '@cratis/scene.primereact/primeReactTheme.css';");
    [Fact] void should_keep_the_icons() => Content(".frontend/main.tsx").ShouldContain("import 'primeicons/primeicons.css';");
    [Fact] void should_keep_the_styling_plugin() => Content(".frontend/vite.config.ts").ShouldContain("styledPrimeReact(),");
    [Fact] void should_keep_the_license_define() => Content(".frontend/vite.config.ts").ShouldContain("'import.meta.env.STAGE_PRIMEUI_LICENSE': JSON.stringify(process.env.STAGE_PRIMEUI_LICENSE || process.env.PRIMEUI_LICENSE || '')");
    [Fact] void should_install_the_scene_prime_adapter() => Content("package.json").ShouldContain("\"@cratis/scene.primereact\"");
    [Fact] void should_install_prime_icons() => Content("package.json").ShouldContain("\"primeicons\"");
    [Fact] void should_keep_the_cratis_baseline() => Content("package.json").ShouldContain("\"@cratis/scene.components\"");
}
