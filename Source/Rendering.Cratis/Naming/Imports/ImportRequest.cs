// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis.Naming.Imports;

/// <summary>
/// Represents everything the <see cref="ImportResolver"/> needs to know about one generated file to decide its using
/// directives and how to spell each type it names.
/// </summary>
/// <param name="FileNamespace">The file-scoped namespace of the file.</param>
/// <param name="Project">The project the file compiles into; exactly one of <see cref="GeneratedProjects.Application"/> and <see cref="GeneratedProjects.Specifications"/>.</param>
/// <param name="GeneratedTypes">The complete renderer-owned declaration inventory, including known scaffold types; user customizations are not inputs to the planner.</param>
/// <param name="References">The types the file names.</param>
internal sealed record ImportRequest(
    string FileNamespace,
    GeneratedProjects Project,
    IEnumerable<GeneratedTypeName> GeneratedTypes,
    IEnumerable<TypeReference> References)
{
    /// <summary>
    /// Gets the namespaces the file must import whatever the references need, such as the namespace of an extension
    /// method the file calls.
    /// </summary>
    public IEnumerable<string> RequiredImports { get; init; } = [];

    /// <summary>
    /// Gets the type parameter and nested type names in scope in the file's types, including nested types inherited
    /// from base classes. They hide a type of the same name everywhere.
    /// </summary>
    public IEnumerable<string> ScopeTypeNames { get; init; } = [];

    /// <summary>
    /// Gets the member, parameter and local names in scope in the file's types, including inherited members. They hide a
    /// type of the same name in an expression.
    /// </summary>
    public IEnumerable<string> ScopeValueNames { get; init; } = [];

    /// <summary>
    /// Gets the simple names the file writes that the resolver does not spell, such as those of an authored body or of
    /// a same-namespace type. An import must not bring a type of any of these names into scope.
    /// </summary>
    public IEnumerable<string> PinnedNames { get; init; } = [];

    /// <summary>
    /// Gets the member names the file accesses with a dot or a null-conditional dot. An import must not bring an
    /// extension member of any of these names into scope, since it could change what a call binds to.
    /// </summary>
    public IEnumerable<string> MemberAccessNames { get; init; } = [];

    /// <summary>
    /// Gets a value indicating whether the names in scope cannot be determined, for instance because a type derives from
    /// a class the catalog does not record or because an authored body could not be read. The file then keeps every
    /// reference fully qualified and gains no import.
    /// </summary>
    public bool HasUnknownScope { get; init; }

    /// <summary>
    /// Gets a value indicating whether the file is rendered for an application whose scaffold-owned GlobalUsings.cs is part of
    /// the render. When <see langword="false"/> the destination may lack that file or have edited it, so the file also imports
    /// every scaffold global namespace it relies on; the output then compiles with and without the file.
    /// </summary>
    public bool HasScaffoldGlobalUsings { get; init; } = true;
}
