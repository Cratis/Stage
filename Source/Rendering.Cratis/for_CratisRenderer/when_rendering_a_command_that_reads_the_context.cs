// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using Cratis.Arc;
using Cratis.Arc.Authorization;
using Cratis.Arc.Http;
using Cratis.Arc.Tenancy;
using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Identities;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer.given;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRenderer;

public class when_rendering_a_command_that_reads_the_context : an_application_reading_the_context
{
    IReadOnlyList<string> _errors = null!;
    string _handler = null!;
    Dictionary<string, string> _tenants = null!;
    List<Exception> _collisions = null!;
    string _withoutHeader = string.Empty;

    async Task Because()
    {
        await _renderer.Render([_application], _targetDirectory, _output, _error);
        _errors = RenderedOutput.Errors(_codeOutput.Files);
        _handler = _codeOutput.Files.Single(file => file.RelativePath.EndsWith("Register.cs", StringComparison.Ordinal)).Content;
        if (_errors.Count > 0) return;
        var assembly = RenderedOutput.Load(_codeOutput.Files);
        var commandType = assembly.GetTypes().Single(type => type.Name == "RegisterInvoice");
        var command = Activator.CreateInstance(commandType, "invoice")!;
        var handle = commandType.GetMethod("Handle")!;
        var tenants = Substitute.For<ITenantIdAccessor>();
        var identities = Substitute.For<IIdentityProvider>();
        identities.GetCurrent().Returns(Identity.NotSet);
        var causations = Substitute.For<ICausationManager>();
        causations.GetCurrentChain().Returns([Causation.Unknown()]);
        var principals = Substitute.For<ICurrentPrincipalAccessor>();
        string Apply(string name)
        {
            tenants.Current.Returns(new TenantId(name));
            var produced = handle.Invoke(command, [tenants, identities, causations, principals])!;
            return (string)produced.GetType().GetProperty("RegisteredFor")!.GetValue(produced)!;
        }
        _tenants = new[] { TenantId.Default.Value, TenantId.NotSet.Value, "North", "default" }.ToDictionary(name => name, Apply);
        _collisions = [.. new[] { string.Empty, "00000000-0000-0000-0000-000000000000" }.Select(name => Catch.Exception(() => Apply(name)))];
        var request = Substitute.For<IHttpRequestContext>();
        request.Headers.Returns(new Dictionary<string, string>());
        var requests = Substitute.For<IHttpRequestContextAccessor>();
        requests.Current.Returns(request);
        var headerTenant = new TenantIdAccessor(new HeaderTenantIdResolver(requests, Options.Create(new ArcOptions())));
        var withoutHeader = handle.Invoke(command, [headerTenant, identities, causations, principals])!;
        _withoutHeader = (string)withoutHeader.GetType().GetProperty("RegisteredFor")!.GetValue(withoutHeader)!;
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
    [Fact] void should_translate_the_tenant_from_the_tenant_accessor() => _handler.ShouldContain("global::AcmeBilling.GeneratedTenancy.PortableTenantValues.Translate(tenants.Current.Value");
    [Fact] void should_translate_default_in_the_generated_handler() => _tenants[TenantId.Default.Value].ShouldEqual(Screenplay.Contexts.TenantId.Default.Value);
    [Fact] void should_translate_arcs_unset_tenant_to_the_portable_default() => _tenants[TenantId.NotSet.Value].ShouldEqual(Screenplay.Contexts.TenantId.Default.Value);
    [Fact] void should_use_the_portable_default_for_a_request_without_a_tenant_header() => _withoutHeader.ShouldEqual(Screenplay.Contexts.TenantId.Default.Value);
    [Fact] void should_preserve_a_named_tenant_in_the_generated_handler() => _tenants["North"].ShouldEqual("North");
    [Fact] void should_preserve_named_tenant_casing_in_the_generated_handler() => _tenants["default"].ShouldEqual("default");
    [Fact] void should_reject_ambiguous_tenants_in_the_generated_handler() => _collisions.TrueForAll(error => error.InnerException?.GetType().Name == "AmbiguousTenant").ShouldBeTrue();
    [Fact] void should_read_a_claim_from_the_calling_principal() =>
        _handler.ShouldContain("principals.Current?.FindFirst(\"department\")?.Value");
}
