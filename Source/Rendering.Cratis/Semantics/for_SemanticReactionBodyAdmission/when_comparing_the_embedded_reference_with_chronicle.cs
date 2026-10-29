// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticReactionBodyAdmission;

public class when_comparing_the_embedded_reference_with_chronicle : Specification
{
    Version _embeddedVersion = null!;

    void Because()
    {
        var compilation = CSharpCompilation.Create("ChronicleReferenceVersion", references: [SemanticGenerationBodyAdmission.ChronicleReference]);
        var symbol = (IAssemblySymbol)compilation.GetAssemblyOrModuleSymbol(SemanticGenerationBodyAdmission.ChronicleReference)!;
        _embeddedVersion = symbol.Identity.Version;
    }

    [Fact] void should_match_the_chronicle_assembly_pinned_by_the_package_version() => _embeddedVersion.ShouldEqual(typeof(global::Cratis.Chronicle.Events.EventContext).Assembly.GetName().Version);
}
#endif
