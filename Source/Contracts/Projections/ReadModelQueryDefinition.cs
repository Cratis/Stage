// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Contracts.Projections;

/// <summary>
/// Represents a modeled query over a read model - its name, the parameter it is looked up by and whether it returns
/// a collection.
/// </summary>
/// <param name="Name">The modeled query name, for example <c language="csharp">CommentsForWorkItem</c>.</param>
/// <param name="Parameter">The read-model property the query is narrowed by (its <c language="csharp">by</c> parameter), or <see langword="null"/> when the query takes no argument.</param>
/// <param name="IsCollection">Whether the query returns every matching instance rather than a single one.</param>
public record ReadModelQueryDefinition(
    string Name,
    string? Parameter,
    bool IsCollection);
