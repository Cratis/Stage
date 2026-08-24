// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Renders one semantic specification into focused Cratis scenario-family artifacts.
/// </summary>
internal static class SemanticSpecificationArtifactRenderer
{
    /// <summary>
    /// Renders the generated specification artifacts.
    /// </summary>
    /// <param name="specification">The semantic specification.</param>
    /// <param name="context">The indexed semantic application.</param>
    /// <returns>The generated specification source files.</returns>
    public static IReadOnlyList<RenderedFile> Render(
        SemanticSpecification specification,
        SemanticApplicationContext context)
    {
        var files = new List<RenderedFile>
        {
            SemanticCommandSpecificationRenderer.Render(specification, context)
        };

        files.AddRange(specification.ThenReadModels.Select(_ =>
            SemanticReadModelSpecificationRenderer.Render(specification, _, context)));
        files.AddRange(specification.ThenQueries.Select(_ =>
            SemanticQuerySpecificationRenderer.Render(specification, _, context)));
        return files;
    }
}
