// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Stage.Contracts.Screenplay;

/// <summary>
/// Resolves specification examples before syntax-based Stage consumers read fixture names and values.
/// </summary>
public static class SpecificationExpansion
{
    /// <summary>
    /// Expands the specifications of an application, refusing unresolved examples.
    /// </summary>
    /// <param name="application">The authored application.</param>
    /// <returns>The application with effective specifications.</returns>
    /// <exception cref="InvalidEventModel">The examples cannot be resolved.</exception>
    public static ApplicationSyntax Expand(ApplicationSyntax application)
    {
        var expanded = SpecificationExamples.Expand(application);
        EnsureAccepted(expanded.Diagnostics, "<specifications>");
        return expanded.Application;
    }

    /// <summary>
    /// Expands a specification in its declaration scope, including standalone document examples.
    /// </summary>
    /// <param name="specification">The authored specification.</param>
    /// <param name="application">The surrounding declarations and shared examples.</param>
    /// <param name="scope">The module, nested features, and slice of the use site.</param>
    /// <returns>The effective specification.</returns>
    /// <exception cref="InvalidEventModel">The examples cannot be resolved.</exception>
    public static SpecificationSyntax Expand(SpecificationSyntax specification, ApplicationSyntax application, IReadOnlyList<string> scope)
    {
        var expanded = SpecificationExamples.Expand(specification, application, scope);
        EnsureAccepted(expanded.Diagnostics, string.Join('.', scope));
        return expanded.Value!.Effective;
    }

    static void EnsureAccepted(IEnumerable<Diagnostic> diagnostics, string path)
    {
        var errors = diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ToArray();
        if (errors.Length > 0)
        {
            throw new InvalidEventModel(path, errors.Select(error => $"{error.Code} ({error.Location.Line},{error.Location.Column}): {error.Message}"));
        }
    }
}
