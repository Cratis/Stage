// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Templates;

/// <summary>
/// Resolves semantic identities without embedding target-qualified names.
/// </summary>
public interface ITypeDirectory
{
    /// <summary>
    /// Looks up a semantic identity.
    /// </summary>
    /// <param name="semanticId">Stable semantic identity.</param>
    /// <param name="symbol">Resolved symbol.</param>
    /// <returns>Whether the identity is known.</returns>
    bool TryGet(string semanticId, out TemplateSymbol symbol);
}
