// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer.given;
using Cratis.Stage.Rendering.Cratis.Renderers;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRenderer;

public class when_refusing_protected_event_source_identities : a_multi_slice_application
{
    static readonly SourceLocation _identityLocation = SourceLocation.Start with { Path = "Protected.play", Line = 12, Column = 9 };

    [Theory]
    [InlineData("pii", "identifier", "command identifier")]
    [InlineData("sensitive", "identifier", "command identifier")]
    [InlineData("pii", "destination", "production destination")]
    [InlineData("sensitive", "destination", "production destination")]
    [InlineData("pii", "destination-context", "production destination")]
    [InlineData("sensitive", "destination-context", "production destination")]
    [InlineData("pii", "destination-template", "production destination")]
    [InlineData("sensitive", "destination-template", "production destination")]
    [InlineData("pii", "projection", "projection key")]
    [InlineData("sensitive", "projection", "projection key")]
    [InlineData("pii", "inline-key", "projection key")]
    [InlineData("sensitive", "inline-key", "projection key")]
    [InlineData("pii", "projection-key", "projection key")]
    [InlineData("sensitive", "projection-key", "projection key")]
    public async Task should_reject_before_scaffolding_or_writing_any_artifact_on_every_public_entrypoint(string attribute, string identity, string useKind)
    {
        Configure(attribute, identity);
        var module = _application.Modules.Single();
        var feature = module.Features.Single();
        var context = new ApplicationSet([_application]);
        var selected = feature.Slices.Single(slice => slice.Name == "Protected");
        foreach (var scope in new[] { "application", "module", "feature", "slice" })
        {
            var error = await Catch.Exception(() => scope switch
            {
                "application" => _renderer.Render([_application], _targetDirectory, _output, _error),
                "module" => _renderer.Render(module, context, _targetDirectory, _output, _error),
                "feature" => _renderer.Render(feature, context, _targetDirectory, _output, _error, module: "Billing"),
                _ => _renderer.Render(selected, context, _targetDirectory, _output, _error, module: "Billing", feature: "Invoices"),
            });
            error.ShouldBeOfExactType<RenderingFailed>();
            var failure = ((RenderingFailed)error).Failures.Single();
            failure.ShouldBeOfExactType<UnsupportedProtectedEventSourceIdentity>();
            var rejection = (UnsupportedProtectedEventSourceIdentity)failure;
            rejection.ConceptName.ShouldEqual("ProtectedValue");
            rejection.AttributeName.ShouldEqual(attribute);
            rejection.Location.ShouldEqual(_identityLocation);
            rejection.Message.ShouldContain(UnsupportedProtectedEventSourceIdentity.DiagnosticCode);
            rejection.Message.ShouldContain("surrogate Uuid identifier");
            rejection.IdentityUse.ShouldContain("Billing.Invoices.Protected");
            rejection.IdentityUse.ShouldContain(useKind);
            _error.ToString().ShouldContain(rejection.Message);
            _scaffolder.WasCalled.ShouldBeFalse();
            _codeOutput.Files.ShouldBeEmpty();
            _codeOutput.FailureMarkerWasWritten.ShouldBeFalse();
            _output.ToString().ShouldBeEmpty();
        }
    }

    [Theory]
    [InlineData("pii", "identifier")]
    [InlineData("sensitive", "identifier")]
    [InlineData("pii", "destination")]
    [InlineData("sensitive", "destination")]
    [InlineData("pii", "projection")]
    [InlineData("sensitive", "projection")]
    public void should_also_guard_direct_concept_and_slice_rendering(string attribute, string identity)
    {
        Configure(attribute, identity);
        var context = new ApplicationSet([_application]);
        var selected = context.Slices.Single(slice => slice.Slice.Name == "Protected");
        var conceptError = Catch.Exception(() => ConceptRenderer.Render(context.Concepts["ProtectedValue"], context, "Generated"));
        conceptError.ShouldBeOfExactType<UnsupportedProtectedEventSourceIdentity>();
        ISliceRenderer renderer = selected.Slice.Type == SliceType.StateChange ? new StateChangeSliceRenderer() : new StateViewSliceRenderer();
        var sliceError = Catch.Exception(() => renderer.Render(selected, context, "Generated"));
        sliceError.ShouldBeOfExactType<UnsupportedProtectedEventSourceIdentity>();
    }

    [Fact]
    public async Task should_allow_a_safe_selected_slice_with_an_unselected_protected_identity()
    {
        Configure("pii", "identifier");
        var context = new ApplicationSet([_application]);
        var selected = context.Slices.Single(slice => slice.Slice.Name == "Safe");
        await _renderer.Render(selected.Slice, context, _targetDirectory, _output, _error, module: "Billing", feature: "Invoices");
        _scaffolder.WasCalled.ShouldBeTrue();
        _codeOutput.Files.Count.ShouldEqual(1);
        RenderedOutput.Errors(_codeOutput.Files).ShouldBeEmpty();
        _error.ToString().ShouldBeEmpty();
    }

    [Theory]
    [InlineData("pii")]
    [InlineData("sensitive")]
    public async Task should_preserve_non_identity_compliance_through_application_preflight(string attribute)
    {
        Configure(attribute, "value");
        await _renderer.Render([_application], _targetDirectory, _output, _error);
        _scaffolder.WasCalled.ShouldBeTrue();
        _codeOutput.Files.ShouldContain(file => file.Content.Contains("[PII]", StringComparison.Ordinal));
        _codeOutput.Files.ShouldNotContain(file => file.Content.Contains("NotAudited", StringComparison.Ordinal));
        RenderedOutput.Errors(_codeOutput.Files).ShouldBeEmpty();
        _codeOutput.FailureMarkerWasWritten.ShouldBeFalse();
        _output.ToString().ShouldContain("Rendering complete.");
    }

    void Configure(string attribute, string identity)
    {
        var concept = new ConceptSyntax("ProtectedValue", "String", [new ConceptAttributeSyntax(attribute, SourceLocation.Start)], [], SourceLocation.Start);
        var protectedProperty = new PropertySyntax("protectedValue", new TypeRefSyntax(concept.Name, false, false, SourceLocation.Start), _identityLocation, IsIdentifier: identity == "identifier");
        var plainIdentifier = new PropertySyntax("id", new TypeRefSyntax("Uuid", false, false, SourceLocation.Start), SourceLocation.Start, IsIdentifier: identity != "identifier");
        var produced = new ProducesSyntax("Changed", null, [], SourceLocation.Start)
        {
            For = identity switch
            {
                "destination" => new PathExpressionSyntax("protectedValue", _identityLocation),
                "destination-context" => new ContextExpressionSyntax("command.protectedValue", _identityLocation),
                "destination-template" => new TemplateExpressionSyntax([new TemplateInterpolationSyntax(new PathExpressionSyntax("protectedValue", _identityLocation), _identityLocation)], _identityLocation),
                _ => null,
            },
        };
        var command = new CommandSyntax("Change", [plainIdentifier, protectedProperty], null, [], [produced], null, SourceLocation.Start);
        var declared = new EventSyntax("Changed", [protectedProperty with { IsIdentifier = false }], SourceLocation.Start);
        var key = new PathExpressionSyntax("protectedValue", _identityLocation);
        var from = new FromSyntax(
            [new EventSpecSyntax("Changed", identity == "inline-key" ? key : null, SourceLocation.Start)],
            identity == "projection" ? new ExpressionKeySyntax(key, _identityLocation) : null,
            null,
            [],
            SourceLocation.Start);
        var projection = new ProjectionSyntax("View", "View", null, AutoMapMode.Enabled, identity == "projection-key" ? new ExpressionKeySyntax(key, _identityLocation) : null, [from], SourceLocation.Start);
        var isProjection = string.Equals(identity, "projection", StringComparison.Ordinal) || string.Equals(identity, "inline-key", StringComparison.Ordinal) || string.Equals(identity, "projection-key", StringComparison.Ordinal);
        var blocked = new SliceSyntax(
            isProjection ? SliceType.StateView : SliceType.StateChange,
            "Protected",
            [declared],
            isProjection ? [] : [command],
            [],
            isProjection ? [projection] : [],
            [],
            [],
            [],
            [],
            [],
            SourceLocation.Start);
        var safeCommand = new CommandSyntax("Archive", [], null, [], [new ProducesSyntax("Archived", null, [], SourceLocation.Start)], null, SourceLocation.Start);
        var safe = new SliceSyntax(SliceType.StateChange, "Safe", [new EventSyntax("Archived", [], SourceLocation.Start)], [safeCommand], [], [], [], [], [], [], [], SourceLocation.Start);
        var feature = new FeatureSyntax("Invoices", [], [safe, blocked], SourceLocation.Start);
        var module = new ModuleSyntax("Billing", [], [feature], SourceLocation.Start);
        _application = new ApplicationSyntax([], [concept], [], [module], SourceLocation.Start);
    }
}
