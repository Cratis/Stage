// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Naming;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Renders the specification that proves Chronicle admits the decision reads of every protected command.
/// </summary>
internal static class SemanticProtectedReadAdmissionRenderer
{
    /// <summary>
    /// The folder-relative path of the generated specification.
    /// </summary>
    internal const string Path = "GeneratedCommands/when_admitting_protected_reads.cs";

    /// <summary>
    /// Renders the admission specification.
    /// </summary>
    /// <param name="context">The indexed semantic application.</param>
    /// <param name="commands">The selected commands.</param>
    /// <returns>The generated Debug-only specification.</returns>
    internal static RenderedFile Render(SemanticApplicationContext context, IEnumerable<SemanticCommand> commands)
    {
        var types = new SemanticTypeSystem(context);
        var protectedTypes = commands.Where(command => !command.Reads.IsDefaultOrEmpty)
            .Select(command => $"typeof({types.SliceType(command.Id, command.Name)})");
        var builder = new CSharpCodeBuilder().Namespace($"{context.RootNamespace}.GeneratedCommands.when_admitting_protected_reads")
            .Summary("Proves that Chronicle admits the decision reads of every protected command.")
            .OpenBlock("public class when_admitting_protected_reads : global::Cratis.Specifications.Specification")
            .Line($"[global::Xunit.FactAttribute] void should_admit_the_decision_reads() => global::Cratis.Arc.Chronicle.Testing.Commands.DecisionReadAdmissions.ShouldAdmitDecisionReadsOf({string.Join(", ", protectedTypes)});")
            .EndBlock();

        return new(Path, $"#if DEBUG\n{builder}\n#endif\n");
    }
}
