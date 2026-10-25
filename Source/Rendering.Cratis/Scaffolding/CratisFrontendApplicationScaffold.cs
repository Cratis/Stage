// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Scene.Engine.Packages;
using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Stage.Rendering.Cratis.Scaffolding;

/// <summary>
/// Creates the deterministic in-memory inputs for the framework frontend shell of a first-run Cratis application.
/// </summary>
/// <remarks>
/// The shell mounts the Stage frontend runtime - the same modules a live Stage renders with, installed as the
/// <c language="json">@cratis/stage.runtime</c> package pinned to the Stage release that rendered the application - over the
/// Scene, routes and strings the planner emits in <c language="shell">src/stage.ts</c>. Its other packages are pinned by
/// <see cref="CratisFrontendPackageSet"/> to the versions the runtime declares as peers.
/// </remarks>
public sealed class CratisFrontendApplicationScaffold
{
    /// <summary>
    /// The generated module that loads the application's approved custom packages, present only when it has some.
    /// </summary>
    internal const string ApprovedPackagesModule = ".frontend/approvedPackages.ts";

    /// <summary>
    /// The planned path of the approved package set document the generated frontend loads.
    /// </summary>
    internal const string ApprovedPackagesDocument = "approved-packages.json";

    /// <summary>
    /// Creates the complete frontend application scaffold without writing to a file system.
    /// </summary>
    /// <param name="request">The validated scaffold request.</param>
    /// <returns>The normalized UTF-8 text inputs of the shell in ordinal relative-path order.</returns>
    /// <exception cref="InvalidCratisBackendApplicationScaffold">Thrown when the request is missing.</exception>
    public ImmutableArray<ArtifactRenderInput> Create(CratisBackendApplicationScaffoldRequest request)
    {
        if (request is null)
        {
            throw new InvalidCratisBackendApplicationScaffold("A frontend application scaffold requires a request.");
        }

        var profile = request.Profile;
        var approved = request.ApprovedPackages;
        var artifacts = new List<(string RelativePath, string Content)>
        {
            (".frontend/index.css", IndexCss()),
            (".frontend/index.html", IndexHtml(request)),
            (".frontend/main.tsx", MainModule(!approved.IsEmpty, request.UsesPrimeReact)),
            (".frontend/tsconfig.json", FrontendTsConfig()),
            (".frontend/tsconfig.node.json", NodeTsConfig()),
            (".frontend/vite-env.d.ts", ViteEnvironment()),
            (".frontend/vite.config.ts", ViteConfig(request.UsesPrimeReact)),
            (".gitignore", GitIgnore()),
            (".yarnrc.yml", "nodeLinker: node-modules"),
            ("package.json", PackageJson(request)),
            ("tsconfig.json", RootTsConfig())
        };

        if (!approved.IsEmpty)
        {
            artifacts.Add((ApprovedPackagesDocument, ApprovedPackageSetJson.Write(approved)));
            artifacts.Add((ApprovedPackagesModule, ApprovedPackagesSource(approved)));
        }

        return
        [
            .. artifacts
                .OrderBy(artifact => artifact.RelativePath, StringComparer.Ordinal)
                .Select(artifact => Input(artifact.RelativePath, profile.Version, artifact.Content))
        ];
    }

    internal static IReadOnlyList<string> ShippedNpmPackages(CratisFrontendPackageSet frontend) =>
        [.. Dependencies(frontend, true).Select(_ => _.Name), .. DevDependencies(frontend).Select(_ => _.Name)];

    static ArtifactRenderInput Input(string relativePath, string version, string content)
    {
        var withSingleTrailingLineFeed = $"{content.TrimEnd('\r', '\n')}\n";
        return CratisArtifactRenderInput.CreateText(relativePath, version, withSingleTrailingLineFeed);
    }

    static (string Name, string Version)[] Dependencies(CratisFrontendPackageSet frontend, bool usesPrimeReact) =>
        [.. new (string Name, string Version)[]
    {
        ("@cratis/arc", frontend.ArcPackageVersion),
        ("@cratis/arc.react", frontend.ArcReactPackageVersion),
        ("@cratis/components", frontend.ComponentsPackageVersion),
        ("@cratis/fundamentals", frontend.FundamentalsPackageVersion),
        ("@cratis/scene.blueprint.default", frontend.ScenePackageVersion),
        ("@cratis/scene.components", frontend.ScenePackageVersion),
        ("@cratis/scene.engine", frontend.ScenePackageVersion),
        ("@cratis/scene.model", frontend.ScenePackageVersion),
        ("@cratis/scene.primereact", frontend.ScenePackageVersion),
        ("@cratis/scene.react", frontend.ScenePackageVersion),
        ("@cratis/stage.runtime", ScaffoldPackageVersions.StageRuntime),
        ("@primereact/core", frontend.PrimeReactPackageVersion),
        ("@primereact/headless", frontend.PrimeReactPackageVersion),
        ("@primereact/hooks", frontend.PrimeReactPackageVersion),
        ("@primereact/styles", frontend.PrimeReactPackageVersion),
        ("@primereact/types", frontend.PrimeReactPackageVersion),
        ("@primereact/ui", frontend.PrimeReactPackageVersion),
        ("@primeuix/themes", frontend.PrimeUixThemesPackageVersion),
        ("primeicons", frontend.PrimeIconsPackageVersion),
        ("lucide-react", frontend.LucideReactPackageVersion),
        ("primereact", frontend.PrimeReactPackageVersion),
        ("react", frontend.ReactPackageVersion),
        ("react-dom", frontend.ReactDomPackageVersion),
        ("react-router-dom", frontend.ReactRouterDomPackageVersion),
        ("reflect-metadata", frontend.ReflectMetadataPackageVersion),
        ("rxjs", frontend.RxjsPackageVersion),
        ("tsyringe", frontend.TsyringePackageVersion)
    }.Where(package => usesPrimeReact || !IsPrimeReactPackage(package.Name))];

    static bool IsPrimeReactPackage(string name) =>
        name.StartsWith("@primereact/", StringComparison.Ordinal) ||
        name.StartsWith("@primeuix/", StringComparison.Ordinal) ||
        string.Equals(name, "@cratis/scene.primereact", StringComparison.Ordinal) ||
        string.Equals(name, "primereact", StringComparison.Ordinal) ||
        string.Equals(name, "primeicons", StringComparison.Ordinal);

    static (string Name, string Version)[] DevDependencies(CratisFrontendPackageSet frontend) =>
    [
        ("@cratis/arc.vite", frontend.ArcVitePackageVersion),
        ("@types/node", frontend.TypesNodePackageVersion),
        ("@types/react", frontend.TypesReactPackageVersion),
        ("@types/react-dom", frontend.TypesReactDomPackageVersion),
        ("@vitejs/plugin-react", frontend.VitePluginReactPackageVersion),
        ("typescript", frontend.TypeScriptPackageVersion),
        ("vite", frontend.VitePackageVersion)
    ];

    static string PackageJson(CratisBackendApplicationScaffoldRequest request)
    {
        var frontend = request.Profile.FrontendPackageSet;
        var dependencies = Dependencies(frontend, request.UsesPrimeReact)
            .Concat(request.ApprovedPackages.Packages.Select(package => (Name: package.Module.Npm, package.Module.Version)))
            .OrderBy(_ => _.Name, StringComparer.Ordinal);
        return $$"""
        {
          "name": "{{request.ApplicationName.ToLowerInvariant()}}",
          "private": true,
          "version": "0.0.0",
          "type": "module",
          "packageManager": "yarn@4.18.1",
          "scripts": {
            "dev": "vite --config .frontend/vite.config.ts",
            "build": "tsc -b .frontend/tsconfig.json && vite build --config .frontend/vite.config.ts",
            "preview": "vite preview --config .frontend/vite.config.ts"
          },
          "dependencies": {
        {{string.Join(",\n", dependencies.Select(_ => $"    \"{_.Name}\": \"{_.Version}\""))}}
          },
          "devDependencies": {
        {{string.Join(",\n", DevDependencies(frontend).Select(_ => $"    \"{_.Name}\": \"{_.Version}\""))}}
          }
        }
        """;
    }

    static string GitIgnore() =>
        """
        # Build results
        [Bb]in/
        [Oo]bj/

        # .NET
        project.lock.json
        project.fragment.lock.json
        artifacts/

        # Node.js
        node_modules/
        npm-debug.log*
        yarn-debug.log*
        yarn-error.log*

        # Build output
        wwwroot/
        dist/

        # IDE
        .vs/
        .vscode/
        .idea/

        # OS
        .DS_Store
        Thumbs.db
        """;

    static string IndexHtml(CratisBackendApplicationScaffoldRequest request) =>
        $"""
        <!DOCTYPE html>
        <html lang="en">

        <head>
            <meta charset="UTF-8" />
            <meta name="viewport" content="width=device-width, initial-scale=1.0" />
            <meta name="base-path" content="" />
            <title>{request.ApplicationName}</title>
        </head>

        <body>
            <div id="root"></div>
            <script type="module" src="/main.tsx"></script>
        </body>

        </html>
        """;

    static string MainModule(bool withApprovedPackages, bool usesPrimeReact)
    {
        var main = MainModuleSource(usesPrimeReact);
        return withApprovedPackages ? ApprovedMainModule(main) : main;
    }

    // The packages the application approved are loaded before the first render, through the same loader and
    // diagnostics a React host uses. A package that does not load stops the application with its reason on the
    // page; a profile is never rendered with a silently missing package.
    static string ApprovedMainModule(string main) => main
        .Replace(
            "} from '@cratis/stage.runtime';",
            "} from '@cratis/stage.runtime';\nimport { loadApprovedComponents } from './approvedPackages';",
            StringComparison.Ordinal)
        .Replace(
            "// A lazy CSS import orders",
            "const approved = await loadApprovedComponents();\n\n// A lazy CSS import orders",
            StringComparison.Ordinal)
        .Replace(
            "<App components={cratisComponentsRuntimePackage.components} />",
            "<App components={{ ...approved.components, ...cratisComponentsRuntimePackage.components }} packageCatalog={approved.catalog} screenTemplates={approved.screenTemplates} dialogTemplates={approved.dialogTemplates} />",
            StringComparison.Ordinal);

    static string MainModuleSource(bool usesPrimeReact) => usesPrimeReact ? PrimeReactMainModuleSource() : DefaultMainModuleSource();

    static string DefaultMainModuleSource() =>
        """
        import 'reflect-metadata';
        import './index.css';
        import React from 'react';
        import ReactDOM from 'react-dom/client';
        import { Arc } from '@cratis/arc.react';
        import { CratisComponentsProvider } from '@cratis/components';
        // The runtime entry: the components, their descriptors and the manifest, without the designers, property
        // editors and design-time actions a running application never loads.
        import { cratisComponentsRuntimePackage } from '@cratis/scene.components/runtime';
        import {
            App,
            StageSourceProvider,
            staticStageSource,
            type StageStaticContent,
        } from '@cratis/stage.runtime';

        // The screens are composed, not written here: `src/stage` holds the Scene this application was rendered
        // with, the routes its generated proxies registered and its strings, and `@cratis/stage.runtime` is the same
        // runtime a live Stage renders that Scene with. Editing this file to add a screen would put it outside the
        // composition the backend was generated from.
        // A first-run scaffold has no Scene module yet. An eager glob keeps that frontend buildable and loads
        // the real module, including its bindings and proxy routes, as soon as an application render emits it.
        const stages = import.meta.glob<StageStaticContent>('../src/stage.ts', { eager: true, import: 'stage' });
        const stage = stages['../src/stage.ts'] ?? {
            scene: { layouts: [], screenTemplates: [], screens: [] },
            routes: { commands: {}, queries: {} },
            strings: {},
        };
        const source = staticStageSource(stage);

        // A lazy CSS import orders user-owned tokens after the managed styles, before React renders.
        const customStyles = import.meta.glob('../Customizations/styles.css');
        await Object.values(customStyles)[0]?.();

        ReactDOM.createRoot(document.getElementById('root')!).render(
            <React.StrictMode>
                <CratisComponentsProvider>
                    <Arc>
                        <StageSourceProvider source={source}>
                            <App components={cratisComponentsRuntimePackage.components} />
                        </StageSourceProvider>
                    </Arc>
                </CratisComponentsProvider>
            </React.StrictMode>
        );
        """;

    static string PrimeReactMainModuleSource() =>
        """
        import 'reflect-metadata';
        import './index.css';
        import 'primeicons/primeicons.css';
        import '@cratis/scene.primereact/primeReactTheme.css';
        import React from 'react';
        import ReactDOM from 'react-dom/client';
        import { Arc } from '@cratis/arc.react';
        import { CratisComponentsProvider } from '@cratis/components';
        import { PrimeReactProvider } from '@primereact/core/config';
        // The runtime entry: the components, their descriptors and the manifest, without the designers, property
        // editors and design-time actions a running application never loads.
        import { cratisComponentsRuntimePackage } from '@cratis/scene.components/runtime';
        import {
            App,
            StageSourceProvider,
            registerStageLibrary,
            staticStageSource,
            type StageStaticContent,
        } from '@cratis/stage.runtime';
        import { primeReactLibrary, stageTheme } from '@cratis/stage.runtime/primereact';

        // The screens are composed, not written here: `src/stage` holds the Scene this application was rendered
        // with, the routes its generated proxies registered and its strings, and `@cratis/stage.runtime` is the same
        // runtime a live Stage renders that Scene with. Editing this file to add a screen would put it outside the
        // composition the backend was generated from.
        // A first-run scaffold has no Scene module yet. An eager glob keeps that frontend buildable and loads
        // the real module, including its bindings and proxy routes, as soon as an application render emits it.
        const stages = import.meta.glob<StageStaticContent>('../src/stage.ts', { eager: true, import: 'stage' });
        const stage = stages['../src/stage.ts'] ?? {
            scene: { layouts: [], screenTemplates: [], screens: [] },
            routes: { commands: {}, queries: {} },
            strings: {},
        };
        const source = staticStageSource(stage);

        // PrimeReact 11 verifies a license key at runtime and otherwise shows its unlicensed presentation. The key
        // comes from PRIMEUI_LICENSE (or STAGE_PRIMEUI_LICENSE) when the application is built - see vite.config.ts.
        const primeUiLicense = import.meta.env.STAGE_PRIMEUI_LICENSE || '';

        // A lazy CSS import orders user-owned tokens after the managed styles, before React renders.
        const customStyles = import.meta.glob('../Customizations/styles.css');
        await Object.values(customStyles)[0]?.();

        registerStageLibrary('PrimeReact', primeReactLibrary);

        ReactDOM.createRoot(document.getElementById('root')!).render(
            <React.StrictMode>
                <PrimeReactProvider license={primeUiLicense} theme={stageTheme}>
                    <CratisComponentsProvider>
                        <Arc>
                            <StageSourceProvider source={source}>
                                <App components={cratisComponentsRuntimePackage.components} />
                            </StageSourceProvider>
                        </Arc>
                    </CratisComponentsProvider>
                </PrimeReactProvider>
            </React.StrictMode>
        );
        """;

    static string ApprovedPackagesSource(ApprovedPackageSet approved)
    {
        var styles = approved.Packages.SelectMany(package => package.Module.Styles).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal);
        var importers = approved.Packages.Select(package => package.Module.Npm).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal);
        return $$"""
        // Generated from the approved package set (approved-packages.json): the custom runtime packages this application was approved to render
        // with. Each is installed by package.json and imported here by name - a bundler resolves literal imports at build
        // time, so this is exactly the approved set and nothing a scene can name at runtime.
        import { loadApprovedPackages, mergePackageRegistries, type ApprovedPackageSet } from '@cratis/scene.react';
        {{string.Join('\n', styles.Select(style => $"import '{style}';"))}}

        // The canonical content of approved-packages.json, embedded so the build needs no JSON module resolution.
        const approvedPackageSet = {{ApprovedPackageSetJson.Write(approved).TrimEnd()}} as unknown as ApprovedPackageSet;

        const importers = {
        {{string.Join(",\n", importers.Select(npm => $"    // @ts-ignore - an approved package may ship without type declarations\n    '{npm}': () => import('{npm}')"))}}
        };

        /**
         * Loads the approved packages' component registries and what each declares. The application built them in, so executable imports are
         * allowed; anything that does not load or does not match its declaration stops the application with the reason
         * on the page rather than rendering a profile with a package missing.
         */
        export async function loadApprovedComponents() {
            const loaded = await loadApprovedPackages(approvedPackageSet, importers, { allowExecutableImports: true });
            if (loaded.diagnostics.length > 0) {
                const message = `Approved packages could not be loaded:\n${loaded.diagnostics.join('\n')}`;
                const root = document.getElementById('root');
                if (root) {
                    root.setAttribute('role', 'alert');
                    root.style.whiteSpace = 'pre-wrap';
                    root.textContent = message;
                }

                throw new Error(message);
            }

            return {
                components: mergePackageRegistries(loaded.bundles),
                // What each approved package declares, so an authored `Package.component` resolves to the key it registers under.
                catalog: Object.fromEntries(loaded.bundles.map(bundle => [bundle.manifest.name, bundle.manifest.components])),
                // The screen and dialog templates a domain blueprint among them brings: what a `template blueprint ...` reference fills.
                screenTemplates: loaded.bundles.flatMap(bundle => bundle.screenTemplates ?? []),
                dialogTemplates: loaded.bundles.flatMap(bundle => bundle.dialogTemplates ?? []),
            };
        }
        """;
    }

    /// <summary>
    /// Declares Vite's client types - <c language="typescript">import.meta.glob</c>, <c language="typescript">import.meta.env</c>
    /// and side-effect stylesheet imports - for the application's own modules.
    /// </summary>
    /// <returns>The declaration file content.</returns>
    static string ViteEnvironment() =>
        """
        /// <reference types="vite/client" />

        """;

    static string IndexCss() =>
        """
        @import '@cratis/components/tokens';
        @import '@cratis/components/styles';

        :root {
          font-family: Inter, Avenir, Helvetica, Arial, sans-serif;
          font-size: 14px;
          line-height: 1.5;
          font-weight: 400;

          font-synthesis: none;
          text-rendering: optimizeLegibility;
          -webkit-font-smoothing: antialiased;
          -moz-osx-font-smoothing: grayscale;
        }

        body {
          margin: 0;
          min-width: 320px;
          min-height: 100vh;
        }

        #root {
          height: 100vh;
        }
        """;

    static string ViteConfig(bool usesPrimeReact) => usesPrimeReact ? PrimeReactViteConfig() : DefaultViteConfig();

    static string DefaultViteConfig() =>
        """
        import { defineConfig, type PluginOption } from 'vite';
        import react from '@vitejs/plugin-react';
        import { fileURLToPath } from 'node:url';
        import { EmitMetadataPlugin } from '@cratis/arc.vite';

        export default defineConfig({
            root: fileURLToPath(new URL('./', import.meta.url)),
            envPrefix: 'STAGE_',
            optimizeDeps: {
                exclude: ['tslib'],
            },
            build: {
                outDir: '../wwwroot',
                modulePreload: false,
                target: 'esnext',
                minify: false,
                cssCodeSplit: false,
            },
            plugins: [
                react(),
                EmitMetadataPlugin({ tsconfigPath: fileURLToPath(new URL('./tsconfig.json', import.meta.url)) }) as PluginOption,
            ],
            server: {
                port: 9000,
                open: true,
                proxy: {
                    '/.cratis': {
                        target: 'http://localhost:5000',
                        ws: true
                    },
                    '/api': {
                        target: 'http://localhost:5000',
                        ws: true
                    },
                    '/swagger': {
                        target: 'http://localhost:5000',
                        ws: true
                    }
                }
            }
        });
        """;

    static string PrimeReactViteConfig() =>
        """
        import { defineConfig, type PluginOption } from 'vite';
        import react from '@vitejs/plugin-react';
        import { fileURLToPath } from 'node:url';
        import { EmitMetadataPlugin } from '@cratis/arc.vite';
        import { styledPrimeReact } from '@cratis/stage.runtime/vite';

        export default defineConfig({
            root: fileURLToPath(new URL('./', import.meta.url)),
            envPrefix: 'STAGE_',
            define: {
                'import.meta.env.STAGE_PRIMEUI_LICENSE': JSON.stringify(process.env.STAGE_PRIMEUI_LICENSE || process.env.PRIMEUI_LICENSE || ''),
            },
            optimizeDeps: {
                exclude: ['tslib'],
            },
            build: {
                outDir: '../wwwroot',
                modulePreload: false,
                target: 'esnext',
                minify: false,
                cssCodeSplit: false,
            },
            plugins: [
                styledPrimeReact(),
                react(),
                EmitMetadataPlugin({ tsconfigPath: fileURLToPath(new URL('./tsconfig.json', import.meta.url)) }) as PluginOption,
            ],
            server: {
                port: 9000,
                open: true,
                proxy: {
                    '/.cratis': {
                        target: 'http://localhost:5000',
                        ws: true
                    },
                    '/api': {
                        target: 'http://localhost:5000',
                        ws: true
                    },
                    '/swagger': {
                        target: 'http://localhost:5000',
                        ws: true
                    }
                }
            }
        });
        """;

    static string FrontendTsConfig() =>
        """
        {
            "compilerOptions": {
                "target": "ES2022",
                "useDefineForClassFields": false,
                "lib": [
                    "ES2022",
                    "DOM",
                    "DOM.Iterable"
                ],
                "module": "ESNext",
                "skipLibCheck": true,
                "moduleResolution": "bundler",
                "allowImportingTsExtensions": true,
                "isolatedModules": true,
                "moduleDetection": "force",
                "noEmit": true,
                "jsx": "react-jsx",
                "experimentalDecorators": true,
                "emitDecoratorMetadata": true,
                "strict": true,
                "noUnusedLocals": true,
                "noUnusedParameters": true,
                "noFallthroughCasesInSwitch": true
            },
            "references": [
                {
                    "path": "./tsconfig.node.json"
                }
            ],
            "include": [
                "./**/*.ts",
                "./**/*.tsx",
                "../**/*.ts",
                "../**/*.tsx"
            ],
            "exclude": [
                "vite.config.ts",
                "../node_modules/**",
                "../wwwroot/**"
            ]
        }
        """;

    static string NodeTsConfig() =>
        """
        {
            "compilerOptions": {
                "composite": true,
                "skipLibCheck": true,
                "module": "ESNext",
                "moduleResolution": "bundler",
                "allowSyntheticDefaultImports": true,
                "strict": true,
                "types": ["node"]
            },
            "include": ["vite.config.ts"]
        }
        """;

    static string RootTsConfig() =>
        """
        {
            "extends": "./.frontend/tsconfig.json"
        }
        """;
}
