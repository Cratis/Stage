// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Xunit;

namespace Cratis.Stage.Host.for_SemanticRuntime.given;

public static class compiled_plan
{
    public static SemanticExecutionPlan From(string source)
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Model"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("model"), "model", "Model.play", source);
        var compilation = new SemanticModelCompiler().Compile("Model", SemanticDocumentSet.Create([document], catalog));
        Assert.True(compilation.Success, string.Join(Environment.NewLine, compilation.Diagnostics.Select(diagnostic => $"{diagnostic.Code} {diagnostic.Location}: {diagnostic.Message}")));
        return SemanticExecutionPlan.Compile(compilation.Value!.Model).Plan!;
    }
}
