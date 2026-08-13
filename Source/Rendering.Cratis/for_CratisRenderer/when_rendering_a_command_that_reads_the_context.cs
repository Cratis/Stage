// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRenderer;

public class when_rendering_a_command_that_reads_the_context : an_application_reading_the_context
{
    IReadOnlyList<string> _errors = null!;
    string _handler = null!;

    async Task Because()
    {
        await _renderer.Render([_application], _targetDirectory, _output, _error);
        _errors = RenderedOutput.Errors(_codeOutput.Files);
        _handler = _codeOutput.Files.Single(file => file.RelativePath.EndsWith("Register.cs", StringComparison.Ordinal)).Content;
    }

    [Fact] void should_render_an_application_that_compiles() => _errors.ShouldBeEmpty();
    [Fact] void should_not_report_anything_as_unreachable() => _error.ToString().ShouldEqual(string.Empty);
    [Fact] void should_ask_for_the_identity_by_its_full_name() =>
        _handler.ShouldContain("Cratis.Chronicle.Identities.IIdentityProvider identities");
    [Fact] void should_ask_for_each_collaborator_once_however_often_it_is_read() =>
        _handler.ShouldContain(
            "public InvoiceRegistered Handle(ITenantIdAccessor tenants, Cratis.Chronicle.Identities.IIdentityProvider identities, " +
            "ICausationManager causations, ICurrentPrincipalAccessor principals)");
    [Fact] void should_not_ask_for_arcs_command_context() => _handler.ShouldNotContain("CommandContext");
    [Fact] void should_read_the_time_the_command_was_handled() => _handler.ShouldContain("DateTimeOffset.UtcNow");
    [Fact] void should_read_the_tenant_from_the_tenant_accessor() => _handler.ShouldContain("tenants.Current.Value");
    [Fact] void should_read_a_claim_from_the_calling_principal() =>
        _handler.ShouldContain("principals.Current?.FindFirst(\"department\")?.Value");
}
