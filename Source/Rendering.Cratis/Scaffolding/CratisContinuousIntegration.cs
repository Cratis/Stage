// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis.Scaffolding;

/// <summary>
/// Creates the continuous integration files of an application whose root is the repository root.
/// </summary>
static class CratisContinuousIntegration
{
    /// <summary>
    /// The relative path of the build workflow.
    /// </summary>
    public const string WorkflowPath = ".github/workflows/build.yml";

    /// <summary>
    /// The relative path of the dependency update configuration that keeps the workflow's pinned actions current.
    /// </summary>
    public const string DependabotPath = ".github/dependabot.yml";

    /// <summary>
    /// The path prefix that marks a scaffold input as a repository-root file.
    /// </summary>
    public const string Directory = ".github/";

    public static string Workflow(CratisBackendApplicationScaffoldRequest request)
    {
        var project = request.ProjectName;
        var dotnetVersion = $"{request.Profile.TargetFramework[3..]}.x";
        return $$$"""
            name: Build

            on:
              push:
                branches: [main]
              pull_request:
              workflow_dispatch:

            permissions:
              contents: read

            concurrency:
              group: ${{ github.workflow }}-${{ github.ref }}
              cancel-in-progress: true

            jobs:
              backend:
                name: Backend build and specifications
                runs-on: ubuntu-latest
                timeout-minutes: 30
                steps:
                  - uses: {{{CratisContinuousIntegrationActions.Checkout}}}
                  - uses: {{{CratisContinuousIntegrationActions.SetupDotnet}}}
                    with:
                      dotnet-version: {{{dotnetVersion}}}
                  - name: Restore
                    run: dotnet restore {{{project}}}.slnx
                  - name: Build Debug
                    run: dotnet build {{{project}}}.slnx -c Debug --no-restore
                  - name: Run specifications
                    run: dotnet test {{{project}}}.csproj -c Debug --no-build
                  # Release references no test packages, so it restores its own package set.
                  - name: Build Release
                    run: dotnet build {{{project}}}.slnx -c Release

              frontend:
                name: Frontend build
                runs-on: ubuntu-latest
                timeout-minutes: 30
                steps:
                  - uses: {{{CratisContinuousIntegrationActions.Checkout}}}
                  - uses: {{{CratisContinuousIntegrationActions.SetupDotnet}}}
                    with:
                      dotnet-version: {{{dotnetVersion}}}
                  - uses: {{{CratisContinuousIntegrationActions.SetupNode}}}
                    with:
                      node-version: {{{CratisContinuousIntegrationActions.NodeVersion}}}
                  - name: Generate the Arc proxies
                    run: dotnet build {{{project}}}.csproj -c Debug
                  - name: Install frontend dependencies
                    run: npm install --no-audit --no-fund
                  - name: Build the frontend
                    run: npm run build
            """;
    }

    public static string Dependabot() =>
        """
        version: 2
        updates:
          - package-ecosystem: github-actions
            directory: /
            schedule:
              interval: weekly
        """;
}
