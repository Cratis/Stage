// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;

namespace Cratis.Stage.Contracts.Screenplay.for_CommandConverter.given;

public class a_command_syntax : Specification
{
    protected const string SlicePath = "Invoicing.InvoiceManagement.RegisterInvoice";

    protected SchemaSynthesizer _schema = null!;

    void Establish() => _schema = new(new Dictionary<string, ConceptSyntax>(StringComparer.Ordinal));

    // The Screenplay compiler reports a second identifier and drops it, so a command carrying more than one can only
    // be built straight as syntax - which is exactly what the converter has to hold up against.
    protected static CommandSyntax Command(params (string Name, bool IsIdentifier)[] properties) =>
        new(
            "RegisterInvoice",
            [.. properties.Select(property => new PropertySyntax(
                property.Name,
                new TypeRefSyntax("Uuid", false, false, SourceLocation.Start),
                SourceLocation.Start,
                property.IsIdentifier))],
            null,
            [],
            [],
            null,
            SourceLocation.Start);
}
