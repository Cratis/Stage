// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics;
using Xunit;

namespace Cratis.Stage.Conformance.Specs.for_ThreeWayConformance.given;

internal static class nested_checksum_cases
{
    internal static ExecutableSemanticModel Compile(bool optional)
    {
        var source = $$"""
            concept AccountCode : String
              validate
                checksum luhn message "Invalid account code"
            type Details
              code AccountCode{{(optional ? " optional" : "")}}
            module Inputs
              feature Checking
                slice StateChange Check
                  command CheckInput
                    id String
                    details Details
                    returns id
                  specification ValidChecksum
                    when CheckInput
                      id = "one"
                      details = {"code":"79927398713"}
                    then returns "one"
                  specification InvalidChecksum
                    when CheckInput
                      id = "one"
                      details = {"code":"79927398714"}
                    then error "Invalid account code"
            """;
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("NestedChecksums"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("checksums"), "checksums", "Checksums.play", source);
        var compilation = new SemanticModelCompiler().Compile("NestedChecksums", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join(';', compilation.Diagnostics));
        return compilation.Value!.Model;
    }
}
#endif
