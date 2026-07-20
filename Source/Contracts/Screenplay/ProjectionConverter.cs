// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;
using Cratis.Stage.Contracts.Projections;

namespace Cratis.Stage.Contracts.Screenplay;

/// <summary>
/// Converts a Screenplay <see cref="ProjectionSyntax"/> into a Stage <see cref="ProjectionDefinition"/> — which is
/// deliberately shaped to be compatible with Chronicle's projection engine. Mirrors Chronicle's own projection syntax
/// visitor so the translated <c>from</c>/<c>join</c>/<c>children</c>/<c>every</c>/<c>remove</c> blocks are interpreted
/// identically by the engine at runtime.
/// </summary>
/// <remarks>
/// Two Screenplay constructs have no representation in Stage's projection model and are dropped: <c>nested</c> (a single
/// nullable child object) and the projection-level <c>key</c> declaration (Chronicle's visitor likewise ignores it in
/// favor of block-level keys). An <c>all</c> block maps to a from-every block with children included; Stage cannot flag
/// "subscribes to all events" (the runtime hard-codes that off), so a pure <c>all</c> projection degrades to its mappings.
/// </remarks>
public static class ProjectionConverter
{
    /// <summary>
    /// Converts a projection declaration into its Stage definition.
    /// </summary>
    /// <param name="projection">The projection to convert.</param>
    /// <returns>The Stage projection definition.</returns>
    public static ProjectionDefinition Convert(ProjectionSyntax projection)
    {
        var context = Process(projection.Blocks);

        return new ProjectionDefinition(
            IsActive: true,
            IsRewindable: false,
            InitialModelState: "{}",
            From: context.From,
            Join: context.Join,
            Children: context.Children,
            FromDerivatives: [],
            FromEvery: context.BuildEvery(),
            FromEventProperty: null,
            RemovedWith: context.RemovedWith,
            RemovedWithJoin: context.RemovedWithJoin,
            Tags: [],
            AutoMap: projection.AutoMap == AutoMapMode.Disabled ? ProjectionAutoMap.Disabled : ProjectionAutoMap.Enabled);
    }

    static BlockContext Process(IEnumerable<ProjectionBlockSyntax> blocks)
    {
        var context = new BlockContext();

        foreach (var block in blocks)
        {
            switch (block)
            {
                case FromSyntax from:
                    ProcessFrom(from, context);
                    break;
                case EverySyntax every:
                    context.EveryProperties.AddRange(Mappings(every.Mappings));
                    context.EveryIncludeChildren = every.IncludeChildren;
                    context.EveryAutoMap = every.AutoMap;
                    break;
                case AllSyntax all:
                    context.EveryProperties.AddRange(Mappings(all.Mappings));
                    context.EveryIncludeChildren = true;
                    context.EveryAutoMap = all.AutoMap;
                    break;
                case JoinSyntax join:
                    ProcessJoin(join, context);
                    break;
                case ChildrenSyntax children:
                    ProcessChildren(children, context);
                    break;
                case RemoveWithSyntax removeWith:
                    context.RemovedWith[removeWith.Event] = new RemovedWithDefinition(KeyOrEmpty(removeWith.Key), KeyOrNull(removeWith.ParentKey));
                    break;
                case RemoveViaJoinSyntax removeViaJoin:
                    context.RemovedWithJoin[removeViaJoin.Event] = new RemovedWithJoinDefinition(KeyOrEmpty(removeViaJoin.Key));
                    break;
                case ClearWithSyntax clearWith:
                    context.RemovedWith[clearWith.Event] = new RemovedWithDefinition(string.Empty, ParentKey: null);
                    break;
                default:
                    // NestedSyntax (and any future block) has no Stage projection representation — skip.
                    break;
            }
        }

        return context;
    }

    static void ProcessFrom(FromSyntax from, BlockContext context)
    {
        var parentKey = KeyOrNull(from.ParentKey);
        var blockKey = ScreenplayExpression.ToKeyExpression(from.Key);

        foreach (var spec in from.Events)
        {
            var key = spec.Key is not null ? ScreenplayExpression.ToKeyExpression(spec.Key) : blockKey;
            context.From[spec.Event] = new FromDefinition(Mappings(from.Mappings), key, parentKey);
        }
    }

    static void ProcessJoin(JoinSyntax join, BlockContext context)
    {
        foreach (var joinEvent in join.Events)
        {
            context.Join[joinEvent.Event] = new JoinDefinition(join.On, Mappings(joinEvent.Mappings), Key: null);
        }
    }

    static void ProcessChildren(ChildrenSyntax children, BlockContext context)
    {
        var childContext = Process(children.Blocks);
        context.Children[children.Property] = new ChildrenDefinition(
            ScreenplayExpression.ToKeyExpression(children.IdentifiedBy),
            childContext.From,
            childContext.Join,
            childContext.Children,
            childContext.BuildEvery(),
            FromEventProperty: null,
            childContext.RemovedWith,
            childContext.RemovedWithJoin,
            AutoMap: (ProjectionAutoMap)(int)children.AutoMap);
    }

    static IReadOnlyList<PropertyMapping> Mappings(IEnumerable<MappingSyntax> mappings) =>
        [.. mappings.Select(mapping => new PropertyMapping(mapping.Property, mapping switch
        {
            SetMappingSyntax set => ScreenplayExpression.ToProjectionExpression(set.Source),
            AddMappingSyntax add => $"$add({ScreenplayExpression.ToProjectionExpression(add.Value)})",
            SubtractMappingSyntax subtract => $"$subtract({ScreenplayExpression.ToProjectionExpression(subtract.Value)})",
            IncrementMappingSyntax => "$increment",
            DecrementMappingSyntax => "$decrement",
            CountMappingSyntax => "$count",
            _ => string.Empty
        }))];

    static string KeyOrEmpty(ExpressionSyntax? expression) => expression is not null ? ScreenplayExpression.ToKeyExpression(expression) : string.Empty;

    static string? KeyOrNull(ExpressionSyntax? expression) => expression is not null ? ScreenplayExpression.ToKeyExpression(expression) : null;

    sealed class BlockContext
    {
        public Dictionary<string, FromDefinition> From { get; } = [];

        public Dictionary<string, JoinDefinition> Join { get; } = [];

        public Dictionary<string, ChildrenDefinition> Children { get; } = [];

        public Dictionary<string, RemovedWithDefinition> RemovedWith { get; } = [];

        public Dictionary<string, RemovedWithJoinDefinition> RemovedWithJoin { get; } = [];

        public List<PropertyMapping> EveryProperties { get; } = [];

        public bool EveryIncludeChildren { get; set; }

        public AutoMapMode EveryAutoMap { get; set; } = AutoMapMode.Inherit;

        public FromEveryDefinition BuildEvery() => new(EveryProperties, EveryIncludeChildren, (ProjectionAutoMap)(int)EveryAutoMap);
    }
}
