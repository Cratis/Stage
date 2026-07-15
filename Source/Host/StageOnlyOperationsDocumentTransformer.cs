// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Cratis.Stage.Host;

/// <summary>
/// Removes the framework infrastructure operations (identity, development, introspection and query
/// transport — all served under the <c>/.cratis</c> route) from the generated OpenAPI document so the
/// Scalar reference only shows the operations that belong to the event model being played.
/// </summary>
public class StageOnlyOperationsDocumentTransformer : IOpenApiDocumentTransformer
{
    const string FrameworkPathPrefix = "/.cratis";

    /// <summary>
    /// Transforms the document by stripping the framework infrastructure paths.
    /// </summary>
    /// <param name="document">The <see cref="OpenApiDocument"/> to transform.</param>
    /// <param name="context">The <see cref="OpenApiDocumentTransformerContext"/> for the transformation.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        if (document.Paths is null)
        {
            return Task.CompletedTask;
        }

        var frameworkPaths = document.Paths.Keys
            .Where(path => path.StartsWith(FrameworkPathPrefix, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        foreach (var path in frameworkPaths)
        {
            document.Paths.Remove(path);
        }

        // Drop the now-unreferenced top-level tags, otherwise Scalar renders an empty section header for
        // each framework tag (Cratis Identity, Cratis Development, ...) that no longer has any operations.
        if (document.Tags is { Count: > 0 })
        {
            var referencedTags = document.Paths.Values
                .Where(item => item.Operations is not null)
                .SelectMany(item => item.Operations!.Values)
                .Where(operation => operation.Tags is not null)
                .SelectMany(operation => operation.Tags!)
                .Select(tag => tag.Name)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var tag in document.Tags.ToArray())
            {
                if (!referencedTags.Contains(tag.Name))
                {
                    document.Tags.Remove(tag);
                }
            }
        }

        return Task.CompletedTask;
    }
}
