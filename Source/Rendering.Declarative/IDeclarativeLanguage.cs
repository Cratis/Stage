// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Stage.Rendering.Templates;

namespace Cratis.Stage.Rendering.Declarative;

/// <summary>
/// Defines a language pack that the declarative planner binds to a kit, adding file resolution and authored-body admission to the template seam.
/// </summary>
/// <remarks>This seam is prerelease. It mirrors the symbol and native-segment concepts of the language packs, so a pack can implement it without the planner referencing it.</remarks>
public interface IDeclarativeLanguage : ITemplateLanguage, ITypeDirectory
{
    /// <summary>
    /// Resolves declarations, references and the imports block of one file into ordered pieces.
    /// </summary>
    /// <param name="path">The validated relative path of the file.</param>
    /// <param name="segments">The ordered segments of the file.</param>
    /// <returns>The resolved pieces, or an integrity refusal such as an ambiguous short name.</returns>
    /// <remarks>An implementation never emits a fully qualified name.</remarks>
    DeclarativeFileResolution Resolve(string path, ImmutableArray<DeclarativeSegment> segments);

    /// <summary>
    /// Judges one authored body. The body is scanned and never rewritten.
    /// </summary>
    /// <param name="requirementId">The implementation requirement.</param>
    /// <param name="body">The authored body.</param>
    /// <returns>The verdict.</returns>
    AuthoredAdmission Admit(string requirementId, string body);
}
