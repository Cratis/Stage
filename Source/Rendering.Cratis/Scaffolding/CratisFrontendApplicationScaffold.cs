// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Stage.Rendering.Cratis.Scaffolding;

/// <summary>
/// Creates the deterministic in-memory inputs for the framework frontend shell of a first-run Cratis application.
/// </summary>
/// <remarks>
/// The shell carries Scene and the component library the composed screen binds through, because a planned
/// <c language="json">scene.json</c> and its binding module import them. They were left out while the emitted era predated
/// the Scene capabilities a composition needs; the era has since moved, so the packages are pinned by
/// <see cref="CratisFrontendPackageSet"/> yet, and selecting them may require moving that era forward.
/// </remarks>
public sealed class CratisFrontendApplicationScaffold
{
    /// <summary>
    /// Creates the complete frontend application scaffold without writing to a file system.
    /// </summary>
    /// <param name="request">The validated scaffold request.</param>
    /// <returns>Nine normalized UTF-8 text inputs in ordinal relative-path order.</returns>
    /// <exception cref="InvalidCratisBackendApplicationScaffold">Thrown when the request is missing.</exception>
    public ImmutableArray<ArtifactRenderInput> Create(CratisBackendApplicationScaffoldRequest request)
    {
        if (request is null)
        {
            throw new InvalidCratisBackendApplicationScaffold("A frontend application scaffold requires a request.");
        }

        var profile = request.Profile;
        var artifacts = new (string RelativePath, string Content)[]
        {
            (".frontend/index.css", IndexCss()),
            (".frontend/index.html", IndexHtml(request)),
            (".frontend/main.tsx", MainModule()),
            (".frontend/tsconfig.json", FrontendTsConfig()),
            (".frontend/tsconfig.node.json", NodeTsConfig()),
            (".frontend/vite.config.ts", ViteConfig()),
            (".gitignore", GitIgnore()),
            ("package.json", PackageJson(request)),
            ("tsconfig.json", RootTsConfig())
        };

        return
        [
            .. artifacts
                .OrderBy(artifact => artifact.RelativePath, StringComparer.Ordinal)
                .Select(artifact => Input(artifact.RelativePath, profile.Version, artifact.Content))
        ];
    }

    static ArtifactRenderInput Input(string relativePath, string version, string content)
    {
        var withSingleTrailingLineFeed = $"{content.TrimEnd('\r', '\n')}\n";
        return CratisArtifactRenderInput.CreateText(relativePath, version, withSingleTrailingLineFeed);
    }

    static string PackageJson(CratisBackendApplicationScaffoldRequest request)
    {
        var frontend = request.Profile.FrontendPackageSet;
        return $$"""
        {
          "name": "{{request.ApplicationName.ToLowerInvariant()}}",
          "private": true,
          "version": "0.0.0",
          "type": "module",
          "scripts": {
            "dev": "vite --config .frontend/vite.config.ts",
            "build": "tsc -b .frontend/tsconfig.json && vite build --config .frontend/vite.config.ts",
            "preview": "vite preview --config .frontend/vite.config.ts"
          },
          "dependencies": {
            "@cratis/arc": "{{frontend.ArcPackageVersion}}",
            "@cratis/arc.react": "{{frontend.ArcReactPackageVersion}}",
            "@cratis/components": "{{frontend.ComponentsPackageVersion}}",
            "@cratis/fundamentals": "{{frontend.FundamentalsPackageVersion}}",
            "@cratis/scene.components": "{{frontend.ScenePackageVersion}}",
            "@cratis/scene.engine": "{{frontend.ScenePackageVersion}}",
            "@cratis/scene.model": "{{frontend.ScenePackageVersion}}",
            "@cratis/scene.react": "{{frontend.ScenePackageVersion}}",
            "@primereact/core": "{{frontend.PrimeReactPackageVersion}}",
            "@primereact/headless": "{{frontend.PrimeReactPackageVersion}}",
            "@primereact/hooks": "{{frontend.PrimeReactPackageVersion}}",
            "@primereact/styles": "{{frontend.PrimeReactPackageVersion}}",
            "@primereact/types": "{{frontend.PrimeReactPackageVersion}}",
            "@primeuix/themes": "{{frontend.PrimeUixThemesPackageVersion}}",
            "primeicons": "{{frontend.PrimeIconsPackageVersion}}",
            "primereact": "{{frontend.PrimeReactPackageVersion}}",
            "react": "{{frontend.ReactPackageVersion}}",
            "react-dom": "{{frontend.ReactDomPackageVersion}}",
            "react-router-dom": "{{frontend.ReactRouterDomPackageVersion}}",
            "reflect-metadata": "{{frontend.ReflectMetadataPackageVersion}}",
            "rxjs": "{{frontend.RxjsPackageVersion}}",
            "tsyringe": "{{frontend.TsyringePackageVersion}}"
          },
          "devDependencies": {
            "@cratis/arc.vite": "{{frontend.ArcVitePackageVersion}}",
            "@types/node": "{{frontend.TypesNodePackageVersion}}",
            "@types/react": "{{frontend.TypesReactPackageVersion}}",
            "@types/react-dom": "{{frontend.TypesReactDomPackageVersion}}",
            "@vitejs/plugin-react": "{{frontend.VitePluginReactPackageVersion}}",
            "typescript": "{{frontend.TypeScriptPackageVersion}}",
            "vite": "{{frontend.VitePackageVersion}}"
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

    static string MainModule() =>
        """
        import 'reflect-metadata';
        import './index.css';
        import React from 'react';
        import ReactDOM from 'react-dom/client';
        import { Arc } from '@cratis/arc.react';
        import { CratisComponentsProvider } from '@cratis/components';
        import { SceneElementView, coreComponents } from '@cratis/scene.react';
        import { cratisComponentsPackage } from '@cratis/scene.components';
        import type { Screen } from '@cratis/scene.model';
        import composition from '../scene.json';
        import '../src/bindings';

        // The screen is composed, not written here: `scene.json` states what this application shows and
        // `src/bindings` registers the generated proxies under the names it refers to. Editing this file to
        // add a screen would put it outside the composition the backend was generated from.
        //
        // Typed by the screen rather than by a whole-application type: Scene has no "translated application"
        // concept in TypeScript, and inventing one here would not match what the package actually exports.
        const scene = composition as unknown as { screens: Screen[] };
        const screen = scene.screens[0];
        const elements = screen ? Object.values(screen.slotContent).flat() : [];
        const components = { ...coreComponents, ...cratisComponentsPackage.components };

        // A lazy CSS import orders user-owned tokens after the managed styles, before React renders.
        const customStyles = import.meta.glob('../Customizations/styles.css');
        await Object.values(customStyles)[0]?.();

        ReactDOM.createRoot(document.getElementById('root')!).render(
            <React.StrictMode>
                <CratisComponentsProvider>
                    <Arc>
                        <main id="application">
                            {elements.map(element => (
                                <SceneElementView
                                    key={element.id}
                                    element={element}
                                    registry={components}
                                    resolveBinding={() => undefined} />
                            ))}
                        </main>
                    </Arc>
                </CratisComponentsProvider>
            </React.StrictMode>
        );
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

    static string ViteConfig() =>
        """
        import { defineConfig, type PluginOption } from 'vite';
        import react from '@vitejs/plugin-react';
        import { fileURLToPath } from 'node:url';
        import { EmitMetadataPlugin } from '@cratis/arc.vite';

        export default defineConfig({
            root: fileURLToPath(new URL('./', import.meta.url)),
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

    static string FrontendTsConfig() =>
        """
        {
            "compilerOptions": {
                "target": "ES2020",
                "useDefineForClassFields": false,
                "lib": [
                    "ES2020",
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
