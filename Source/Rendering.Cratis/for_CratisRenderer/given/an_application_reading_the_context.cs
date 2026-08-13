// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.Emission;
using Cratis.Stage.Rendering.Cratis.Renderers;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRenderer.given;

/// <summary>
/// A command that fills its event from every <c>$context</c> path the language names, onto event properties
/// declared as the types those values actually are.
/// </summary>
/// <remarks>
/// A command handler is the one place these have nowhere to come from: it receives no event context and Arc's
/// <c>CommandContext</c> carries none of them. Everything the document can say here therefore has to be reached
/// through a collaborator, and the only assertion that proves the reach is real is compiling the result.
/// </remarks>
public class an_application_reading_the_context : Specification
{
    protected InMemoryCodeOutput _codeOutput = null!;
    protected CratisRenderer _renderer = null!;
    protected ApplicationSyntax _application = null!;
    protected DirectoryInfo _targetDirectory = null!;
    protected StringWriter _output = null!;
    protected StringWriter _error = null!;

    void Establish()
    {
        var invoiceNumber = Property("invoiceNumber", "String", isIdentifier: true);

        var registered = new EventSyntax(
            "InvoiceRegistered",
            [
                invoiceNumber,
                Property("registeredAt", "DateTime"),
                Property("registeredFor", "String"),
                Property("registeredBy", "String"),
                Property("registeredByName", "String"),
                Property("registeredByUserName", "String"),
                Property("causedBySubject", "String"),
                Property("causedByName", "String"),
                Property("causedByUserName", "String"),
                Property("causedVia", "String"),
                Property("wasAuthenticated", "Bool"),
                Property("department", "String"),
            ],
            SourceLocation.Start);

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
                        Maps("invoiceNumber", new PathExpressionSyntax("invoiceNumber", SourceLocation.Start)),
                        Maps("registeredAt", Context("occurred")),
                        Maps("registeredFor", Context("tenant")),
                        Maps("registeredBy", Context("identity.id")),
                        Maps("registeredByName", Context("identity.name")),
                        Maps("registeredByUserName", Context("identity.userName")),
                        Maps("causedBySubject", Context("causedBy.subject")),
                        Maps("causedByName", Context("causedBy.name")),
                        Maps("causedByUserName", Context("causedBy.userName")),
                        Maps("causedVia", Context("causation.type")),
                        Maps("wasAuthenticated", Context("identity.isAuthenticated")),
                        Maps("department", Context("identity.claims.department")),
                    ],
                    SourceLocation.Start)
            ],
            null,
            SourceLocation.Start);

        var slice = new SliceSyntax(
            SliceType.StateChange, "Register", [registered], [register], [], [], [], [], [], [], [], SourceLocation.Start);

        var feature = new FeatureSyntax("Invoicing", [], [slice], SourceLocation.Start);
        var module = new ModuleSyntax("Billing", [], [feature], SourceLocation.Start);

        _application = new ApplicationSyntax([], [], [], [module], SourceLocation.Start);

        _codeOutput = new InMemoryCodeOutput();
        _renderer = new CratisRenderer(
            new a_stub_scaffolder(),
            new Dictionary<SliceType, ISliceRenderer> { [SliceType.StateChange] = new StateChangeSliceRenderer() },
            _codeOutput);

        _targetDirectory = new DirectoryInfo(Path.Combine(Path.GetTempPath(), "AcmeBilling"));
        _output = new StringWriter();
        _error = new StringWriter();
    }

    static PropertySyntax Property(string name, string type, bool isIdentifier = false) =>
        new(name, new TypeRefSyntax(type, false, false, SourceLocation.Start), SourceLocation.Start, IsIdentifier: isIdentifier);

    static ContextExpressionSyntax Context(string path) => new(path, SourceLocation.Start);

    static PropertyMappingSyntax Maps(string property, ExpressionSyntax source) => new(property, source, SourceLocation.Start);
}
