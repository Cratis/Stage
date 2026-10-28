// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Stage.Rendering.Declarative;
using Cratis.Stage.Rendering.Templates;

namespace Cratis.Stage.Rendering.Languages.Java;

/// <summary>
/// Holds a validated plan-wide Java declaration registry and deterministic nested-name allocation.
/// </summary>
public sealed class JavaPlan
{
    JavaPlan(ImmutableArray<DeclarativeLanguageFile> files, ImmutableDictionary<TemplateSymbol, JavaDeclaration> declarations)
    {
        Files = files;
        Declarations = declarations;
    }

    /// <summary>
    /// Gets all files in their original order, including non-Java files whose references retain canonical identities.
    /// </summary>
    public ImmutableArray<DeclarativeLanguageFile> Files { get; }

    /// <summary>
    /// Gets one entry per original declaration identity, including its allocated name and owning file.
    /// </summary>
    public ImmutableDictionary<TemplateSymbol, JavaDeclaration> Declarations { get; }

    /// <summary>
    /// Validates every file and allocates nested names without renaming top-level types or changing file order.
    /// </summary>
    /// <param name="files">All mapped files of the plan.</param>
    /// <returns>The immutable plan registry.</returns>
    /// <exception cref="LanguagePackRefusal">A symbol, file structure, plan-wide declaration or nested-name allocation is invalid.</exception>
    public static JavaPlan Create(ImmutableArray<DeclarativeLanguageFile> files)
    {
        var declarations = JavaPlanRegistry.Build(files);
        var words = files.ToImmutableDictionary(file => file.Path, file => JavaWords.Scan(JavaFileStructure.RawText(file.Segments)).All, StringComparer.Ordinal);
        return new(files, JavaDeclarationNames.Allocate(declarations, words));
    }
}
