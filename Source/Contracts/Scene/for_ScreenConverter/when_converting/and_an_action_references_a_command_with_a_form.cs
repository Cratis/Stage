// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_ScreenConverter.when_converting;

public class and_an_action_references_a_command_with_a_form : Specification
{
    ScreenSyntax _syntax = null!;
    List<FormSyntax> _availableForms = null!;
    ScreenConversionResult _result = null!;

    void Establish()
    {
        _syntax = new(
            "InvoiceDetails",
            null,
            [new ScreenActionSyntax("RegisterInvoice", null, null, SourceLocation.Start)],
            SourceLocation.Start);

        _availableForms =
        [
            new FormSyntax("RegisterInvoiceForm", "RegisterInvoice", null, [], null, SourceLocation.Start),
            new FormSyntax("CancelInvoiceForm", "CancelInvoice", null, [], null, SourceLocation.Start),
        ];
    }

    void Because() => _result = ScreenConverter.Convert(_syntax, _availableForms, []);

    [Fact] void should_include_only_the_form_for_the_referenced_command() => _result.Screen.Forms.Count.ShouldEqual(1);
    [Fact] void should_include_the_matching_forms_name() => _result.Screen.Forms[0].Name.ShouldEqual("RegisterInvoiceForm");
}
