// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticReactionBodyAdmission;

public class when_comparing_the_embedded_reference_with_reactive : Specification
{
    Version _embeddedVersion = null!;

    void Because()
    {
        var reference = SemanticGenerationBodyAdmission.ReactiveReference;
        var compilation = CSharpCompilation.Create("ReactiveReferenceVersion", references: [reference]);
        var symbol = (IAssemblySymbol)compilation.GetAssemblyOrModuleSymbol(reference);
        _embeddedVersion = symbol.Identity.Version;
    }

    [Fact] void should_match_the_reactive_assembly_resolved_by_arc_and_chronicle() => _embeddedVersion.ShouldEqual(typeof(System.Reactive.Subjects.Subject<>).Assembly.GetName().Version);
}
#endif
