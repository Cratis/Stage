// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text;
using System.Text.RegularExpressions;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.for_CratisBackendApplicationScaffold.given;
using Cratis.Stage.Rendering.Cratis.Scaffolding;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisBackendApplicationScaffold;

public class when_creating_for_a_repository_root : a_current_scaffold
{
    ImmutableArray<ArtifactRenderInput> _root;
    ImmutableArray<ArtifactRenderInput> _rootAgain;
    string _workflow = null!;
    string _dependabot = null!;
    string _readme = null!;

    void Because()
    {
        var request = _request.WithRepositoryRoot();
        var scaffold = new CratisBackendApplicationScaffold();
        _root = scaffold.Create(request);
        _rootAgain = scaffold.Create(request);
        _workflow = Text(Input(".github/workflows/build.yml"));
        _dependabot = Text(Input(".github/dependabot.yml"));
        _readme = Text(Input("README.md"));
    }

    [Fact] void should_add_only_the_workflow_and_dependabot_files_to_the_default_roster() => _root.Select(PathOf).Order(StringComparer.Ordinal).SequenceEqual(_first.Select(PathOf).Concat([".github/dependabot.yml", ".github/workflows/build.yml"]).Order(StringComparer.Ordinal)).ShouldBeTrue();
    [Fact] void should_keep_ordinal_path_order() => _root.Select(PathOf).SequenceEqual(_root.Select(PathOf).Order(StringComparer.Ordinal)).ShouldBeTrue();
    [Fact] void should_keep_every_default_input_but_the_readme_byte_identical() => _first.Where(input => PathOf(input) != "README.md").All(input => _root.Single(other => other.Name == input.Name).Bytes.SequenceEqual(input.Bytes)).ShouldBeTrue();
    [Fact] void should_version_every_input_with_the_scaffold_contract() => _root.All(input => input.Version == "2").ShouldBeTrue();
    [Fact] void should_use_line_feeds_only() => _root.All(input => !input.Bytes.Contains((byte)'\r')).ShouldBeTrue();
    [Fact] void should_end_the_workflow_with_exactly_one_line_feed() => (_workflow.EndsWith('\n') && !_workflow.EndsWith("\n\n", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_repeat_the_same_bytes() => _rootAgain.Zip(_root).All(pair => pair.First.Name == pair.Second.Name && pair.First.Bytes.SequenceEqual(pair.Second.Bytes)).ShouldBeTrue();
    [Fact] void should_pin_every_action_to_a_full_commit_hash_with_a_release_comment() => Regex.Matches(_workflow, @"uses: (\S+)(.*)").All(match => Regex.IsMatch(match.Groups[1].Value, "^[a-z-]+/[a-z-]+@[0-9a-f]{40}$") && Regex.IsMatch(match.Groups[2].Value, @"^ # v\d+\.\d+\.\d+$")).ShouldBeTrue();
    [Fact] void should_use_an_action_in_every_job() => Regex.Matches(_workflow, "uses:").Count.ShouldEqual(5);
    [Fact] void should_limit_every_job_with_a_timeout() => Regex.Matches(_workflow, "timeout-minutes: 30").Count.ShouldEqual(2);
    [Fact] void should_grant_read_only_contents_permission() => _workflow.ShouldContain("permissions:\n  contents: read\n");
    [Fact] void should_cancel_superseded_runs() => _workflow.ShouldContain("group: ${{ github.workflow }}-${{ github.ref }}\n  cancel-in-progress: true");
    [Fact] void should_run_on_main_pushes_pull_requests_and_manual_dispatch() => _workflow.ShouldContain("on:\n  push:\n    branches: [main]\n  pull_request:\n  workflow_dispatch:\n");
    [Fact] void should_not_run_on_a_schedule() => _workflow.ShouldNotContain("schedule");
    [Fact] void should_not_use_secrets() => _workflow.ShouldNotContain("secrets.");
    [Fact] void should_build_the_solution_for_debug_and_release() => (_workflow.Contains("dotnet build MyApp.slnx -c Debug --no-restore") && _workflow.Contains("dotnet build MyApp.slnx -c Release --no-restore")).ShouldBeTrue();
    [Fact] void should_run_the_specifications_project_without_rebuilding() => _workflow.ShouldContain("dotnet test MyApp.Specs/MyApp.Specs.csproj -c Debug --no-build");
    [Fact] void should_derive_the_sdk_version_from_the_target_framework() => _workflow.ShouldContain("dotnet-version: 10.0.x");
    [Fact] void should_generate_the_proxies_before_building_the_frontend() => _workflow.IndexOf("dotnet build MyApp.csproj", StringComparison.Ordinal).ShouldBeLessThan(_workflow.IndexOf("npm run build", StringComparison.Ordinal));
    [Fact] void should_install_the_frontend_with_npm() => _workflow.ShouldContain("npm install --no-audit --no-fund");
    [Fact] void should_update_only_github_actions_weekly() => _dependabot.ShouldEqual("version: 2\nupdates:\n  - package-ecosystem: github-actions\n    directory: /\n    schedule:\n      interval: weekly\n");
    [Fact] void should_name_npm_in_the_readme() => _readme.ShouldContain("npm run build");
    [Fact] void should_not_name_yarn_in_the_readme() => _readme.ShouldNotContain("yarn");

    ArtifactRenderInput Input(string path) => _root.Single(input => PathOf(input) == path);
}
