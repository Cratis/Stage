// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.Renderers;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_StateChangeSliceRenderer;

/// <summary>
/// The runtime supplies the tenant as a string. A document is free to declare the property it fills as anything —
/// here a <c>Uuid</c> concept — and nothing can turn one into the other, so the mapping is dropped and reported
/// rather than rendered into a conversion that does not exist.
/// </summary>
public class when_the_context_value_cannot_fill_what_it_is_mapped_onto : Specification
{
    CodeGeneration.RenderedFile _file = null!;

    void Because()
    {
        var invoiceNumber = new PropertySyntax(
            "invoiceNumber", new TypeRefSyntax("String", false, false, SourceLocation.Start), SourceLocation.Start, IsIdentifier: true);
        var registeredFor = new PropertySyntax(
            "registeredFor", new TypeRefSyntax("TenantId", false, false, SourceLocation.Start), SourceLocation.Start);

        var registered = new EventSyntax("InvoiceRegistered", [invoiceNumber, registeredFor], SourceLocation.Start);

        var register = new CommandSyntax(
            "RegisterInvoice",
            [invoiceNumber],
            null,
            [],
            [
                new ProducesSyntax(
                    "InvoiceRegistered",
                    null,
                    [
                        new PropertyMappingSyntax("invoiceNumber", new PathExpressionSyntax("invoiceNumber", SourceLocation.Start), SourceLocation.Start),
                        new PropertyMappingSyntax("registeredFor", new ContextExpressionSyntax("tenant", SourceLocation.Start), SourceLocation.Start),
                    ],
                    SourceLocation.Start)
            ],
            null,
            SourceLocation.Start);

        var slice = new SliceSyntax(
            SliceType.StateChange, "Register", [registered], [register], [], [], [], [], [], [], [], SourceLocation.Start);
        var feature = new FeatureSyntax("Invoicing", [], [slice], SourceLocation.Start);
        var module = new ModuleSyntax("Billing", [], [feature], SourceLocation.Start);
        var application = new ApplicationSyntax(
            [], [new ConceptSyntax("TenantId", "Uuid", [], [], SourceLocation.Start)], [], [module], SourceLocation.Start);

        _file = new StateChangeSliceRenderer().Render(
            new LocatedSlice(slice, ["Billing", "Invoicing"]), new ApplicationSet([application]), "Acme");
    }

    [Fact] void should_render_the_mapping_as_a_missing_value() => _file.Content.ShouldContain("public InvoiceRegistered Handle() => new(InvoiceNumber, default!);");
    [Fact] void should_not_ask_for_a_collaborator_it_does_not_use() => _file.Content.ShouldNotContain("ITenantIdAccessor");
    [Fact] void should_say_what_the_document_asked_for_and_what_it_declared() =>
        _file.Diagnostics.ShouldContain(
            "'registeredFor' is mapped from '$context.tenant', a string the runtime supplies, which the event declares as 'TenantId' — a Guid — rendered as a missing value.");
}
