// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer;
using Cratis.Stage.Rendering.Cratis.Renderers;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_StateChangeSliceRenderer;

public class when_declarations_name_files_without_a_file_handler : Specification
{
    LocatedSlice _slice = null!;
    RenderedFile _file = null!;
    object _event = null!;

    void Because()
    {
        const string source = """
            module Billing
              feature Invoices
                slice StateChange Archive
                  file Invoices/Archive.cs
                  command ArchiveInvoice
                    produces InvoiceArchived
                  event InvoiceArchived
                    file Events/InvoiceArchived.cs
            """;
        var compilation = new ScreenplayCompiler().Compile(source);
        compilation.Success.ShouldBeTrue();
        var applicationSet = new ApplicationSet([compilation.Value!]);
        _slice = applicationSet.Slices.Single();
        _file = new StateChangeSliceRenderer().Render(_slice, applicationSet, "CratisApp");
        var assembly = RenderedOutput.Load([_file]);
        var commandType = assembly.GetType("CratisApp.Billing.Invoices.Archive.ArchiveInvoice")!;
        _event = commandType.GetMethod("Handle")!.Invoke(Activator.CreateInstance(commandType), null)!;
    }

    [Fact] void should_keep_the_slice_annotation() => _slice.Slice.File!.Path.ShouldEqual("Invoices/Archive.cs");
    [Fact] void should_keep_the_event_annotation() => _slice.Slice.Events.Single().File!.Path.ShouldEqual("Events/InvoiceArchived.cs");
    [Fact] void should_not_confuse_annotations_with_implementations() => _slice.Slice.Commands.Single().Handler.ShouldBeNull();
    [Fact] void should_compile_the_declarative_output() => RenderedOutput.Errors([_file]).ShouldBeEmpty();
    [Fact] void should_execute_the_declarative_handler() => _event.GetType().FullName.ShouldEqual("CratisApp.Billing.Invoices.Archive.InvoiceArchived");
}
