// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticGenerationBodyAdmission;

public class when_comparing_the_embedded_reference_with_arc : Specification
{
    Version _embeddedVersion = null!;

    void Because()
    {
        var compilation = CSharpCompilation.Create("GenerationReferenceVersion", references: [SemanticGenerationBodyAdmission.FundamentalsReference]);
        var symbol = (IAssemblySymbol)compilation.GetAssemblyOrModuleSymbol(SemanticGenerationBodyAdmission.FundamentalsReference)!;
        _embeddedVersion = symbol.Identity.Version;
    }

    [Fact] void should_match_the_fundamentals_assembly_resolved_by_arc() => _embeddedVersion.ShouldEqual(typeof(global::Cratis.Concepts.GenerationContext).Assembly.GetName().Version);
}
#endif
