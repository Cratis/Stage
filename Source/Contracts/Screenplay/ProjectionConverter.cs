// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;
using Cratis.Stage.Contracts.Projections;

namespace Cratis.Stage.Contracts.Screenplay;

/// <summary>
/// Converts Screenplay projections into Chronicle-compatible Stage definitions.
/// </summary>
public static class ProjectionConverter
{
    /// <summary>
    /// Converts a projection declaration into its Stage definition.
    /// </summary>
    /// <param name="projection">The projection to convert.</param>
    /// <returns>The Stage projection definition.</returns>
    /// <exception cref="UnsupportedProjectionConversion">A syntax construct has no supported translation.</exception>
    public static ProjectionDefinition Convert(ProjectionSyntax projection)
    {
        if (projection.Sequence is not null)
        {
            throw new UnsupportedProjectionConversion($"sequence '{projection.Sequence}'");
        }

        var rootAutoMap = projection.AutoMap == AutoMapMode.Disabled ? ProjectionAutoMap.Disabled : ProjectionAutoMap.Enabled;
        var context = Process(projection.Blocks, rootAutoMap, isChild: false);

        return new ProjectionDefinition(
            IsActive: true,
            IsRewindable: false,
            InitialModelState: "{}",
            From: context.From,
            Join: context.Join,
            Children: context.Children,
            FromDerivatives: [],
            FromEvery: context.Every,
            FromEventProperty: null,
            RemovedWith: context.RemovedWith,
            RemovedWithJoin: context.RemovedWithJoin,
            Tags: [],
            AutoMap: rootAutoMap)
        {
            Nested = context.Nested.Count > 0 ? context.Nested : null,
            SubscribesToAllEvents = context.SubscribesToAllEvents
        };
    }

    static BlockContext Process(IEnumerable<ProjectionBlockSyntax> blocks, ProjectionAutoMap rootAutoMap, bool isChild)
    {
        var context = new BlockContext();
        foreach (var block in blocks)
        {
            switch (block)
            {
                case FromSyntax from:
                    var parentKey = from.ParentKey is null ? null : ProjectionExpression.Key(from.ParentKey);
                    foreach (var spec in from.Events)
                    {
                        context.From[spec.Event] = new FromDefinition(
                            Mappings(from.Mappings),
                            spec.Key is null ? ProjectionExpression.Key(from.Key) : ProjectionExpression.Key(spec.Key),
                            parentKey);
                    }
                    break;
                case EverySyntax every:
                    var mappings = Mappings(every.Mappings);
                    if (isChild)
                    {
                        var merged = context.Every.Properties.ToDictionary(_ => _.Property, _ => _.Expression);
                        foreach (var mapping in mappings)
                        {
                            merged[mapping.Property] = mapping.Expression;
                        }

                        context.Every = context.Every with
                        {
                            Properties = [.. merged.Select(_ => new PropertyMapping(_.Key, _.Value))],
                            AutoMap = every.AutoMap == AutoMapMode.Inherit ? context.Every.AutoMap : Resolve(every.AutoMap, rootAutoMap)
                        };
                    }
                    else
                    {
                        context.Every = new(mappings, every.IncludeChildren, Resolve(every.AutoMap, rootAutoMap));
                    }
                    break;
                case AllSyntax all:
                    context.Every = new(Mappings(all.Mappings), IncludeChildren: true, Resolve(all.AutoMap, rootAutoMap));
                    context.SubscribesToAllEvents = true;
                    break;
                case JoinSyntax join:
                    foreach (var spec in join.Events)
                    {
                        context.Join[spec.Event] = new JoinDefinition(join.On, Mappings(spec.Mappings), string.Empty);
                    }
                    break;
                case ChildrenSyntax children:
                    var child = Process(children.Blocks, rootAutoMap, isChild: true);
                    context.Children[children.Property] = Child(ProjectionExpression.Key(children.IdentifiedBy), child, Resolve(children.AutoMap, rootAutoMap));
                    break;
                case NestedSyntax nested:
                    var nestedContext = Process(nested.Blocks, rootAutoMap, isChild: true);
                    context.Nested[nested.Property] = Child("*NotSet*", nestedContext, Resolve(nested.AutoMap, rootAutoMap));
                    break;
                case RemoveWithSyntax removal:
                    context.RemovedWith[removal.Event] = new(
                        removal.Key is null ? string.Empty : ProjectionExpression.Key(removal.Key),
                        removal.ParentKey is null ? null : ProjectionExpression.Key(removal.ParentKey));
                    break;
                case RemoveViaJoinSyntax removal:
                    context.RemovedWithJoin[removal.Event] = new(removal.Key is null ? string.Empty : ProjectionExpression.Key(removal.Key));
                    break;
                case ClearWithSyntax clear:
                    context.RemovedWith[clear.Event] = new(string.Empty, ParentKey: null);
                    break;
                default:
                    throw new UnsupportedProjectionConversion(block.GetType().Name);
            }
        }

        return context;
    }

    static ChildrenDefinition Child(string identifiedBy, BlockContext context, ProjectionAutoMap autoMap) =>
        new(
            identifiedBy,
            context.From,
            context.Join,
            context.Children,
            context.Every,
            null,
            context.RemovedWith,
            context.RemovedWithJoin,
            autoMap)
        {
            Nested = context.Nested.Count > 0 ? context.Nested : null
        };

    static IReadOnlyList<PropertyMapping> Mappings(IEnumerable<MappingSyntax> mappings)
    {
        var properties = new Dictionary<string, string>();
        foreach (var mapping in mappings)
        {
            properties[mapping.Property] = mapping switch
            {
                SetMappingSyntax { Source: LiteralExpressionSyntax { Value: null } } => "$null",
                SetMappingSyntax set => ProjectionExpression.Value(set.Source),
                ClearMappingSyntax => "$null",
                AddMappingSyntax add => $"$add({ProjectionExpression.Value(add.Value)})",
                SubtractMappingSyntax subtract => $"$subtract({ProjectionExpression.Value(subtract.Value)})",
                IncrementMappingSyntax => "$increment",
                DecrementMappingSyntax => "$decrement",
                CountMappingSyntax => "$count",
                _ => throw new UnsupportedProjectionConversion(mapping.GetType().Name)
            };
        }

        return [.. properties.Select(_ => new PropertyMapping(_.Key, _.Value))];
    }

    static ProjectionAutoMap Resolve(AutoMapMode mode, ProjectionAutoMap rootAutoMap) => mode switch
    {
        AutoMapMode.Enabled => ProjectionAutoMap.Enabled,
        AutoMapMode.Disabled => ProjectionAutoMap.Disabled,
        AutoMapMode.Inherit => rootAutoMap,
        _ => throw new UnsupportedProjectionConversion($"AutoMapMode.{mode}")
    };

    sealed class BlockContext
    {
        public Dictionary<string, FromDefinition> From { get; } = [];
        public Dictionary<string, JoinDefinition> Join { get; } = [];
        public Dictionary<string, ChildrenDefinition> Children { get; } = [];
        public Dictionary<string, ChildrenDefinition> Nested { get; } = [];
        public Dictionary<string, RemovedWithDefinition> RemovedWith { get; } = [];
        public Dictionary<string, RemovedWithJoinDefinition> RemovedWithJoin { get; } = [];
        public FromEveryDefinition Every { get; set; } = new([], IncludeChildren: false);
        public bool SubscribesToAllEvents { get; set; }
    }
}
