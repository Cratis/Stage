// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Stage.Rendering.Cratis;

/// <summary>
/// Represents validated evidence of an existing classic reducer file that a migration to a self-reducing read model removes.
/// </summary>
/// <param name="ReadModel">The semantic identity of the read model being migrated.</param>
/// <param name="RelativePath">The portable application-root-relative path of the existing reducer file.</param>
/// <param name="Sha256">The lowercase SHA-256 of the file's LF-normalized UTF-8 bytes.</param>
/// <param name="ObserverId">The Chronicle observer identity of the existing reducer.</param>
/// <param name="ReadModelType">The read model type the reducer implements the classic reducer interface for, without a global alias qualifier.</param>
public sealed record ReducerMigrationEvidence(SemanticId ReadModel, string RelativePath, string Sha256, string ObserverId, string ReadModelType)
{
    static readonly string[] _reducerAttributeNames =
        ["Reducer", "ReducerAttribute", "Cratis.Chronicle.Reducers.Reducer", "Cratis.Chronicle.Reducers.ReducerAttribute"];

    /// <summary>
    /// Derives migration evidence from the exact bytes of an existing classic reducer file.
    /// </summary>
    /// <param name="readModel">The semantic identity of the read model being migrated.</param>
    /// <param name="relativePath">The application-root-relative path the content was read from.</param>
    /// <param name="content">The existing file content.</param>
    /// <returns>The evidence: the observer identity is the literal reducer attribute identifier, or the reducer's full type name.</returns>
    /// <exception cref="InvalidArtifactRenderContract">
    /// The path is not portable, the file does not parse, does not declare exactly one classic reducer class,
    /// or declares its identifier with a non-literal or empty argument.
    /// </exception>
    public static ReducerMigrationEvidence FromExistingFile(SemanticId readModel, string relativePath, string content)
    {
        if (!readModel.IsSet || content is null)
        {
            throw new InvalidArtifactRenderContract("Reducer migration evidence requires a read model identity and file content.");
        }

        var artifact = PlannedArtifact.CreateText(relativePath, content);
        var root = CSharpSyntaxTree.ParseText(Encoding.UTF8.GetString(artifact.Bytes.AsSpan())).GetCompilationUnitRoot();
        if (root.ContainsDiagnostics)
        {
            throw new InvalidArtifactRenderContract($"Reducer file '{artifact.RelativePath}' does not parse.");
        }

        var reducers = root.DescendantNodes().OfType<ClassDeclarationSyntax>()
            .Select(declaration => (Declaration: declaration, Target: ReducerTarget(declaration)))
            .Where(candidate => candidate.Target is not null).ToArray();
        if (reducers is not [var (declaration, target)])
        {
            throw new InvalidArtifactRenderContract($"Reducer file '{artifact.RelativePath}' must declare exactly one IReducerFor<T> class.");
        }

        return new(readModel, artifact.RelativePath, artifact.Sha256, ObserverIdOf(declaration, artifact.RelativePath), target!);
    }

    static string? ReducerTarget(ClassDeclarationSyntax declaration) =>
        declaration.BaseList?.Types.Select(baseType => baseType.Type switch
        {
            QualifiedNameSyntax { Right: GenericNameSyntax generic } => generic,
            AliasQualifiedNameSyntax { Name: GenericNameSyntax generic } => generic,
            GenericNameSyntax generic => generic,
            _ => null
        }).Where(generic => generic is { TypeArgumentList.Arguments.Count: 1 } &&
            string.Equals(generic.Identifier.ValueText, "IReducerFor", StringComparison.Ordinal))
            .Select(generic => Unqualified(generic!.TypeArgumentList.Arguments[0].ToString()))
            .FirstOrDefault();

    static string ObserverIdOf(ClassDeclarationSyntax declaration, string path)
    {
        var attribute = declaration.AttributeLists.SelectMany(list => list.Attributes)
            .SingleOrDefault(candidate => _reducerAttributeNames.Contains(Unqualified(candidate.Name.ToString()), StringComparer.Ordinal));
        if (attribute?.ArgumentList?.Arguments.FirstOrDefault() is not { NameEquals: null } argument ||
            (argument.NameColon is not null && !string.Equals(argument.NameColon.Name.Identifier.ValueText, "id", StringComparison.Ordinal)))
        {
            return FullName(declaration);
        }

        if (argument.Expression is not LiteralExpressionSyntax literal || !literal.IsKind(SyntaxKind.StringLiteralExpression))
        {
            throw new InvalidArtifactRenderContract($"Reducer file '{path}' declares a non-literal reducer identifier.");
        }

        var id = literal.Token.ValueText;
        return string.IsNullOrWhiteSpace(id) ? FullName(declaration) : id;
    }

    static string FullName(ClassDeclarationSyntax declaration)
    {
        // Chronicle falls back to the CLR full name, which separates nested types with '+'.
        var namespaces = declaration.Ancestors().OfType<BaseNamespaceDeclarationSyntax>().Select(ns => ns.Name.ToString()).Reverse();
        var types = declaration.AncestorsAndSelf().OfType<ClassDeclarationSyntax>().Select(type => type.Identifier.ValueText).Reverse();
        var typeName = string.Join('+', types);
        var ns = string.Join('.', namespaces);
        return ns.Length == 0 ? typeName : $"{ns}.{typeName}";
    }

    static string Unqualified(string name) =>
        string.Concat(name.Where(character => !char.IsWhiteSpace(character))).Replace("global::", string.Empty, StringComparison.Ordinal);
}
