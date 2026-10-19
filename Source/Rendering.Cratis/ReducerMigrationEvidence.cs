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
/// Evidence is only derived from file content through <see cref="FromExistingFile"/>.
/// </summary>
public sealed record ReducerMigrationEvidence
{
    static readonly string[] _reducerAttributeNames = ["Reducer", "ReducerAttribute"];

    static readonly string[] _sequenceAttributeNames =
        ["EventSequence", "EventSequenceAttribute", "EventStore", "EventStoreAttribute"];

    internal ReducerMigrationEvidence(SemanticId readModel, string relativePath, string sha256, string observerId, string readModelType, string? refusal = null)
    {
        ReadModel = readModel;
        RelativePath = relativePath;
        Sha256 = sha256;
        ObserverId = observerId;
        ReadModelType = readModelType;
        Refusal = refusal;
    }

    /// <summary>
    /// Gets the semantic identity of the read model being migrated.
    /// </summary>
    public SemanticId ReadModel { get; internal init; }

    /// <summary>
    /// Gets the portable application-root-relative path of the existing reducer file.
    /// </summary>
    public string RelativePath { get; internal init; }

    /// <summary>
    /// Gets the lowercase SHA-256 of the file's LF-normalized UTF-8 bytes.
    /// </summary>
    public string Sha256 { get; internal init; }

    /// <summary>
    /// Gets the Chronicle observer identity of the existing reducer.
    /// </summary>
    public string ObserverId { get; internal init; }

    /// <summary>
    /// Gets the read model type the reducer implements the classic reducer interface for, without a global alias qualifier.
    /// </summary>
    public string ReadModelType { get; internal init; }

    /// <summary>
    /// Gets why the existing reducer cannot be migrated without changing its observation, or null when it can.
    /// </summary>
    public string? Refusal { get; internal init; }

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

        var (observerId, refusal) = ObserverIdOf(declaration, artifact.RelativePath);
        return new(readModel, artifact.RelativePath, artifact.Sha256, observerId, target!, refusal);
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

    static (string ObserverId, string? Refusal) ObserverIdOf(ClassDeclarationSyntax declaration, string path)
    {
        var attributes = declaration.AttributeLists.SelectMany(list => list.Attributes).ToArray();
        var reducerAttributes = attributes.Where(candidate => IsNamed(candidate, _reducerAttributeNames)).ToArray();
        if (reducerAttributes.Length > 1)
        {
            throw new InvalidArtifactRenderContract($"Reducer file '{path}' declares more than one reducer attribute.");
        }

        var refusal = attributes.Any(candidate => IsNamed(candidate, _sequenceAttributeNames))
            ? "the classic reducer observes a non-default event sequence or event store, which the self-reducing read model does not keep."
            : null;
        var arguments = reducerAttributes.FirstOrDefault()?.ArgumentList?.Arguments ?? default;
        if (arguments.Count == 0)
        {
            return (FullName(declaration), refusal);
        }

        var argument = arguments[0];
        if (arguments.Count > 1 || argument.NameEquals is not null ||
            (argument.NameColon is not null && !string.Equals(argument.NameColon.Name.Identifier.ValueText, "id", StringComparison.Ordinal)))
        {
            return (FullName(declaration), "the classic reducer attribute sets more than its identifier, which the self-reducing read model does not keep.");
        }

        if (argument.Expression is not LiteralExpressionSyntax literal || !literal.IsKind(SyntaxKind.StringLiteralExpression))
        {
            throw new InvalidArtifactRenderContract($"Reducer file '{path}' declares a non-literal reducer identifier.");
        }

        var id = literal.Token.ValueText;
        return (string.IsNullOrWhiteSpace(id) ? FullName(declaration) : id, refusal);
    }

    static bool IsNamed(AttributeSyntax attribute, string[] names)
    {
        var name = Unqualified(attribute.Name.ToString());
        var simple = name[(name.LastIndexOf('.') + 1)..];
        return names.Contains(simple, StringComparer.Ordinal);
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
