// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRenderer;

public class when_rendering_an_application_with_authorization : an_application_with_authorization
{
    IReadOnlyList<string> _compilationErrors = null!;

    async Task Because()
    {
        await _renderer.Render([_application], _targetDirectory, _output, _error);
        _compilationErrors = RenderedOutput.Errors(_codeOutput.Files);
    }

    // Asserted as joined text rather than an empty collection so a failure names the compilation errors.
    [Fact] void should_render_output_that_compiles() =>
        string.Join(Environment.NewLine, _compilationErrors).ShouldEqual(string.Empty);
    [Fact] void should_import_the_authorization_namespace_into_the_command_slice() =>
        SliceContent("Register").ShouldContain("using Cratis.Arc.Authorization;");
    [Fact] void should_guard_the_command_with_the_roles_it_authorizes() =>
        SliceContent("Register").ShouldContain("[Roles(\"Administrator\", \"Auditor\")]");
    [Fact] void should_state_that_a_command_without_authorization_is_anonymous() =>
        SliceContent("Archive").ShouldContain("[AllowAnonymous]");
    [Fact] void should_guard_the_read_model_with_what_its_queries_authorize() =>
        SliceContent("Summary").ShouldContain("[Roles(\"Administrator\", \"Auditor\")]");
    [Fact] void should_report_the_queries_it_does_not_render() =>
        _error.ToString().ShouldContain("Slice 'Summary' declares 2 query declaration(s) with no rendered equivalent");
    [Fact] void should_report_the_constraint_it_does_not_render() =>
        _error.ToString().ShouldContain("Slice 'Register' declares 1 constraint declaration(s) with no rendered equivalent");
    [Fact] void should_report_the_screen_it_does_not_render() =>
        _error.ToString().ShouldContain("Slice 'Summary' declares 1 screen declaration(s) with no rendered equivalent");
    [Fact] void should_report_the_personas_it_does_not_render() =>
        _error.ToString().ShouldContain("1 persona declaration(s) are not rendered");
    [Fact] void should_report_the_authentication_providers_it_does_not_render() =>
        _error.ToString().ShouldContain("1 authentication provider(s) are not rendered");

    string SliceContent(string slice) =>
        _codeOutput.Files.Single(file => file.RelativePath.EndsWith(Path.Combine(slice, $"{slice}.cs"), StringComparison.Ordinal)).Content;
}
