// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticModelCompiler_operations;

public class when_compiling_operations : Specification
{
    const string Prefix = "system Mailer\nmodule M\n  feature F\n    slice StateChange S\n";
    const string SpecificationPrefix = Prefix + "      operation Send\n        uses Mailer\n        compensate\n          description \"Undo sending\"\n      command C\n        produces Send\n      specification T\n";

    [Theory]
    [InlineData("system Mailer\n")]
    [InlineData(Prefix + "      operation Send\n        uses Mailer\n")]
    [InlineData(Prefix + "      command C\n        produces operation Send\n          uses Mailer\n")]
    [InlineData(Prefix + "      operation Send\n        uses Mailer\n        execute\n          file Adapters/Send.cs\n")]
    [InlineData(Prefix + "      operation Send\n        uses Mailer\n        execute\n          ```csharp\n          return;\n          ```\n")]
    [InlineData(SpecificationPrefix + "        given operation Send fails\n        when C\n")]
    [InlineData(SpecificationPrefix + "        when C\n        then operation Send\n")]
    [InlineData(SpecificationPrefix + "        when C\n        then compensated Send\n")]
    public void should_refuse_before_creating_an_executable_model(string source)
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Operations"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("operations"), "operations", "Operations.play", source);
        var compilation = new SemanticModelCompiler().Compile("Operations", SemanticDocumentSet.Create([document], catalog));
        compilation.Success.ShouldBeFalse();
        compilation.Value.ShouldBeNull();
        var errors = compilation.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ToArray();
        errors.ShouldNotBeEmpty();
        errors.All(diagnostic => diagnostic.Code == "PLAY0268" && diagnostic.Message.Contains("not admitted by any supported executable model (ESM) version yet (#301)", StringComparison.Ordinal)).ShouldBeTrue();
    }
}
