// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay;
using Cratis.Screenplay.Syntax;
using Cratis.Stage.Rendering.Cratis.Renderers;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CratisRenderer.given;

public class an_application_with_external_bases : a_multi_slice_application
{
    internal const string Source = """
        concept InvoiceId : Uuid
        concept InvoiceName : String
          validate
            not empty
        module Billing
          feature Invoices
            slice StateChange Register
              command RegisterInvoice
                invoiceId InvoiceId identifier
                name InvoiceName
                validate
                  name not empty
                produces InvoiceRegistered
                  for invoiceId
                  name = name
              event InvoiceRegistered
                name InvoiceName
              specification Registering
                when RegisterInvoice
                  invoiceId = "00000000-0000-0000-0000-000000000001"
                  name = "invoice"
                then InvoiceRegistered
                  name = "invoice"
            slice Automation Notify
              reaction NotifyInvoiceRegistered
                when InvoiceRegistered
                  ```csharp
                  return null;
                  ```
            slice StateView Totals
              readmodel Total
                name InvoiceName
              reducer Fold => Total
                on InvoiceRegistered
                  ```csharp
                  return new Total(context.Event.Name);
                  ```
        """;

    void Establish()
    {
        var compilation = new ScreenplayCompiler().Compile(Source);
        Assert.True(compilation.Success, string.Join(Environment.NewLine, compilation.Diagnostics));
        _application = compilation.Value!;
        _renderer = new CratisRenderer(
            _scaffolder,
            new Dictionary<SliceType, ISliceRenderer>
            {
                [SliceType.StateChange] = new StateChangeSliceRenderer(),
                [SliceType.StateView] = new StateViewSliceRenderer(),
                [SliceType.Automation] = new ReactionSliceRenderer()
            },
            _codeOutput);
    }

    protected string Artifact(string suffix) => _codeOutput.Files.Single(file => file.RelativePath.EndsWith(suffix, StringComparison.Ordinal)).Content;
}
