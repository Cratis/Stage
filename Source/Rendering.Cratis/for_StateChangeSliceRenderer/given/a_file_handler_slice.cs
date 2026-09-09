// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;

namespace Cratis.Stage.Rendering.Cratis.for_StateChangeSliceRenderer.given;

public class a_file_handler_slice : Specification
{
    protected const string SourcePath = "legacy/Batch.play";
    protected ApplicationSet _applicationSet = null!;
    protected LocatedSlice _slice = null!;
    protected CommandSyntax _command = null!;

    void Establish()
    {
        const string source = """
            module Billing
              feature Invoices
                slice StateChange Process
                  command ProcessBatch
                    handler
                      file Handlers/ProcessBatch.cs
            """;
        var compiler = new ScreenplayCompiler();
        compiler.Compile(source).Success.ShouldBeTrue();
        var parsed = compiler.Parse(source, SourcePath);
        parsed.Success.ShouldBeTrue();
        _applicationSet = new([parsed.Value!]);
        _slice = _applicationSet.Slices.Single();
        _command = _slice.Slice.Commands.Single();
    }
}
