// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>Applies the generated scaffold's compiler policy to authenticated bodies, without executing them.</summary>
internal static class SemanticBodyCompilation
{
    internal const string ImplicitUsings = "global using System; global using System.Collections.Generic; global using System.IO; global using System.Linq; global using System.Net.Http; global using System.Threading; global using System.Threading.Tasks;\n";

    internal static CSharpCompilation Create(string name, SyntaxTree tree) => CSharpCompilation.Create(name, [tree], PureTransitionAdmission.AnalysisReferences(), new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, generalDiagnosticOption: ReportDiagnostic.Error, nullableContextOptions: NullableContextOptions.Enable));

    internal static CSharpCompilation Create(string name, IEnumerable<SyntaxTree> trees, IEnumerable<MetadataReference> references, params string[] suppressed) =>
        CSharpCompilation.Create(name, trees, references, new CSharpCompilationOptions(
            OutputKind.DynamicallyLinkedLibrary,
            generalDiagnosticOption: ReportDiagnostic.Error,
            specificDiagnosticOptions: suppressed.ToDictionary(id => id, _ => ReportDiagnostic.Suppress),
            nullableContextOptions: NullableContextOptions.Enable));

    internal static bool IsFailure(Diagnostic diagnostic) => !diagnostic.IsSuppressed &&
        diagnostic.Severity is DiagnosticSeverity.Error or DiagnosticSeverity.Warning;
}
