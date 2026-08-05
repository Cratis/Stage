// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CSharpCodeBuilder;

public class when_building_a_file : Specification
{
    string _result = null!;

    void Because()
    {
        var builder = new CSharpCodeBuilder()
            .Using("System")
            .Using("System")
            .Using("Cratis.Arc")
            .Namespace("MyApp.Billing.Invoices")
            .Summary("Represents a command to register an invoice.")
            .Attribute("Command")
            .OpenBlock("public record RegisterInvoice(Guid InvoiceId, string Name)")
            .ExpressionMember("public InvoiceRegistered Handle()", "new(InvoiceId, Name)")
            .EndBlock();

        _result = builder.ToString();
    }

    [Fact] void should_include_the_license_header() => _result.ShouldContain("Copyright (c) Cratis");
    [Fact] void should_sort_usings_alphabetically() =>
        _result.IndexOf("using Cratis.Arc;", StringComparison.Ordinal).ShouldBeLessThan(_result.IndexOf("using System;", StringComparison.Ordinal));
    [Fact] void should_deduplicate_repeated_usings() => CountOccurrences(_result, "using System;").ShouldEqual(1);
    [Fact] void should_emit_the_namespace() => _result.ShouldContain("namespace MyApp.Billing.Invoices;");
    [Fact] void should_emit_the_summary() => _result.ShouldContain("/// Represents a command to register an invoice.");
    [Fact] void should_emit_the_attribute() => _result.ShouldContain("[Command]");
    [Fact] void should_emit_the_record_signature() => _result.ShouldContain("public record RegisterInvoice(Guid InvoiceId, string Name)");
    [Fact] void should_indent_members_inside_the_block() => _result.ShouldContain("    public InvoiceRegistered Handle() => new(InvoiceId, Name);");

    static int CountOccurrences(string text, string value) => (text.Length - text.Replace(value, string.Empty, StringComparison.Ordinal).Length) / value.Length;
}
