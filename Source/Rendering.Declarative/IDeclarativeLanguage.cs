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
    /// Resolves declarations, references and the imports block of every file of the plan into ordered pieces.
    /// </summary>
    /// <param name="files">Every file of the plan, in output order. The pack sees the whole plan, so it can allocate names across files.</param>
    /// <returns>One resolution per file, in the same order as <paramref name="files"/>: the resolved pieces, or an integrity refusal such as an ambiguous short name.</returns>
    /// <remarks>An implementation never emits a fully qualified name unless that is the only provable spelling. The planner refuses a result whose length differs from the number of files.</remarks>
    ImmutableArray<DeclarativeFileResolution> Resolve(ImmutableArray<DeclarativeLanguageFile> files);

    /// <summary>
    /// Judges one authored body. The body is scanned and never rewritten.
    /// </summary>
    /// <param name="requirementId">The implementation requirement.</param>
    /// <param name="body">The authored body.</param>
    /// <returns>The verdict.</returns>
    AuthoredAdmission Admit(string requirementId, string body);
}
