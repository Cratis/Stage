// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Scene.Screenplay.Forward;
using Cratis.Screenplay;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Scene.for_StageSceneConversion.given;

/// <summary>
/// A model that declares a design system with the theme that implements it, a ui profile, a free screen a slice adopts and one nobody does.
/// </summary>
public class a_model_with_a_design_system_and_free_screens : Specification
{
    protected const string Source =
        """
        design system ShopDesign
          modes light, dark
          default theme Daylight

          tokens
            color
              surface    "page background"
              text       "body text"
              primary    "primary action"
              on-primary "text on a primary action"

          components
            Button
              parts text, background
              variant primary
                text       = color.on-primary
                background = color.primary

        theme Daylight
          implements ShopDesign

          palette
            indigo = "#4338CA"
            white  = "#FFFFFF"
            ink    = "#111827"

          light
            color.surface    = palette.white
            color.text       = palette.ink
            color.primary    = palette.indigo
            color.on-primary = palette.white

          dark
            color.surface    = palette.ink
            color.text       = palette.white
            color.primary    = palette.indigo
            color.on-primary = palette.white

        ui profile Web
          target platform web
          appearance
            color scheme dark

        shape Invoice
          number String
          amount Decimal

        screen InvoiceList
          title "Invoices"
          data Invoice[]
          table Invoice
            column number
            column amount

        screen Unused
          title "Nobody adopts this"
          data Invoice[]

        module Invoicing
          feature Paying
            slice StateView ListInvoices
              readmodel InvoiceRow
                number String
                amount Decimal
              query ListInvoices => InvoiceRow[]
              uses screen InvoiceList
                data Invoice via query ListInvoices
        """;

    protected ApplicationSyntax _application = null!;
    protected SceneApplication _scene = null!;

    void Establish()
    {
        var compiled = new ScreenplayCompiler().Parse(Source);
        Assert.True(compiled.Success, string.Join("; ", compiled.Diagnostics.Select(diagnostic => $"{diagnostic.Code} {diagnostic.Message}")));
        _application = compiled.Value!;
    }
}
