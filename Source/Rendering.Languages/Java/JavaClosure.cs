// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Stage.Rendering.Templates;

namespace Cratis.Stage.Rendering.Languages.Java;

/// <summary>
/// The names that can bind in each Java file besides the file's own pack spellings, as a least fixpoint over the plan.
/// </summary>
/// <param name="Closures">The binder names per file path: declaring words, nested declarations and the names inherited through every referenced type.</param>
/// <param name="Introduced">The binder names per file path that the model or the facts introduce, without the file's own literal declarations.</param>
/// <param name="Refusals">The unknown-type refusals per file path.</param>
sealed record JavaClosure(
    ImmutableDictionary<string, ImmutableHashSet<string>> Closures,
    ImmutableDictionary<string, ImmutableHashSet<string>> Introduced,
    ImmutableDictionary<string, LanguagePackRefusal> Refusals)
{
    internal static JavaClosure Compute(JavaPlan plan, IReadOnlyCollection<JavaFileAnalysis> files, JavaTypeFacts facts)
    {
        var nodes = new Dictionary<string, JavaClosureNode>(StringComparer.Ordinal);
        var refusals = ImmutableDictionary.CreateBuilder<string, LanguagePackRefusal>(StringComparer.Ordinal);
        foreach (var file in files)
        {
            var node = JavaClosureNode.Create(file, plan, facts);
            nodes[file.File.Path] = node;
            if (node.Refusal is not null)
            {
                refusals[file.File.Path] = node.Refusal;
            }
        }

        return new(Solve(nodes, node => node.Fixed), Solve(nodes, node => node.Introduced), refusals.ToImmutable());
    }

    static ImmutableDictionary<string, ImmutableHashSet<string>> Solve(Dictionary<string, JavaClosureNode> nodes, Func<JavaClosureNode, ImmutableHashSet<string>> seed)
    {
        var sets = nodes.ToDictionary(entry => entry.Key, entry => seed(entry.Value), StringComparer.Ordinal);
        bool changed;
        do
        {
            changed = false;
            foreach (var (path, node) in nodes)
            {
                var union = node.Dependencies.Where(sets.ContainsKey).Aggregate(sets[path], (current, dependency) => current.Union(sets[dependency]));
                if (union.Count != sets[path].Count)
                {
                    sets[path] = union;
                    changed = true;
                }
            }
        }
        while (changed);
        return sets.ToImmutableDictionary(StringComparer.Ordinal);
    }
}
