// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Stage.Contracts.Scene;

/// <summary>Retains data candidates until the attachment (notably a table) selects its subject.</summary>
internal sealed record GuardSubject(GuardDeclarations Declarations, IReadOnlyList<string> Scope, IReadOnlyList<TypeRefSyntax> Candidates)
{
    internal GuardFieldType Field(string path) => Candidates.Count == 1
        ? Declarations.Field(Candidates[0], Scope, path)
        : GuardFieldType.Unknown(Candidates.Count == 0 ? "No data subject" : "Ambiguous data subject");

    internal GuardSubject Enter(IEnumerable<ScreenDirectiveSyntax> directives, bool isolated = false)
    {
        var candidates = directives.OfType<ScreenDataSyntax>().Select(data => data.Type).ToArray();
        return this with { Candidates = candidates.Length > 0 || isolated ? candidates : Candidates };
    }

    internal GuardSubject Row(string target)
    {
        var direct = Candidates.Where(candidate => candidate.Name == target).ToArray();
        if (direct.Length > 0 || Candidates.Count != 1) return this with { Candidates = direct };
        var field = Declarations.Property(Candidates[0].Name, Scope, target);
        return field is { Property.Type.IsCollection: true } row
            ? this with { Scope = row.Scope, Candidates = [row.Property.Type with { IsCollection = false }] }
            : this with { Candidates = [] };
    }
}
