// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis.Scaffolding;

/// <summary>
/// Represents a validated request for a first-run Cratis backend application scaffold.
/// </summary>
public sealed class CratisBackendApplicationScaffoldRequest
{
    static readonly string[] _reservedKeywords =
    [
        "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked", "class", "const",
        "continue", "decimal", "default", "delegate", "do", "double", "else", "enum", "event", "explicit", "extern",
        "false", "finally", "fixed", "float", "for", "foreach", "goto", "if", "implicit", "in", "int", "interface",
        "internal", "is", "lock", "long", "namespace", "new", "null", "object", "operator", "out", "override", "params",
        "private", "protected", "public", "readonly", "ref", "return", "sbyte", "sealed", "short", "sizeof", "stackalloc",
        "static", "string", "struct", "switch", "this", "throw", "true", "try", "typeof", "uint", "ulong", "unchecked",
        "unsafe", "ushort", "using", "virtual", "void", "volatile", "while"
    ];

    CratisBackendApplicationScaffoldRequest(
        string applicationName,
        string projectName,
        string rootNamespace,
        CratisBackendApplicationScaffoldProfile profile)
    {
        ApplicationName = applicationName;
        ProjectName = projectName;
        RootNamespace = rootNamespace;
        Profile = profile;
    }

    /// <summary>
    /// Gets the application name used for the Chronicle event store and MongoDB database.
    /// </summary>
    public string ApplicationName { get; }

    /// <summary>
    /// Gets the project name used for the project and solution file names.
    /// </summary>
    public string ProjectName { get; }

    /// <summary>
    /// Gets the root namespace emitted into the application project.
    /// </summary>
    public string RootNamespace { get; }

    /// <summary>
    /// Gets the exact scaffold profile.
    /// </summary>
    public CratisBackendApplicationScaffoldProfile Profile { get; }

    /// <summary>
    /// Creates a request using one name for the application, project, and root namespace.
    /// </summary>
    /// <param name="applicationName">The application, project, and root namespace name.</param>
    /// <param name="profile">The exact profile, or the current profile when omitted.</param>
    /// <returns>The validated request.</returns>
    /// <exception cref="InvalidCratisBackendApplicationScaffold">Thrown when the name is not a valid dot-separated C# identifier.</exception>
    public static CratisBackendApplicationScaffoldRequest Create(
        string applicationName,
        CratisBackendApplicationScaffoldProfile? profile = null) =>
        Create(applicationName, applicationName, applicationName, profile);

    /// <summary>
    /// Creates a validated request with independently selected application, project, and root namespace names.
    /// </summary>
    /// <param name="applicationName">The application name used for persistent stores.</param>
    /// <param name="projectName">The project and solution file name.</param>
    /// <param name="rootNamespace">The generated application's root namespace.</param>
    /// <param name="profile">The exact profile, or the current profile when omitted.</param>
    /// <returns>The validated request.</returns>
    /// <exception cref="InvalidCratisBackendApplicationScaffold">Thrown when a name is not a valid dot-separated C# identifier.</exception>
    public static CratisBackendApplicationScaffoldRequest Create(
        string applicationName,
        string projectName,
        string rootNamespace,
        CratisBackendApplicationScaffoldProfile? profile = null)
    {
        ValidateName(applicationName, "application");
        ValidateName(projectName, "project");
        ValidateName(rootNamespace, "root namespace");

        return new(applicationName, projectName, rootNamespace, profile ?? CratisBackendApplicationScaffoldProfile.Current);
    }

    static void ValidateName(string value, string role)
    {
        if (string.IsNullOrWhiteSpace(value) || !string.Equals(value, value.Trim(), StringComparison.Ordinal) ||
            !value.Split('.').All(IsIdentifier))
        {
            throw new InvalidCratisBackendApplicationScaffold(
                $"The scaffold {role} value '{value}' must be a dot-separated C# identifier without paths or reserved keywords.");
        }
    }

    static bool IsIdentifier(string value) =>
        !string.IsNullOrEmpty(value) &&
        IsIdentifierStart(value[0]) &&
        value.Skip(1).All(IsIdentifierPart) &&
        !_reservedKeywords.Contains(value, StringComparer.Ordinal);

    static bool IsIdentifierStart(char character) =>
        character is '_' or (>= 'A' and <= 'Z') or (>= 'a' and <= 'z');

    static bool IsIdentifierPart(char character) =>
        IsIdentifierStart(character) || character is >= '0' and <= '9';
}
