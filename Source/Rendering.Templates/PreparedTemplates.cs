// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Stage.Rendering.Templates;

/// <summary>Immutable validated templates. Concurrent sessions do not share evaluation state.</summary>
public sealed class PreparedTemplates
{
    readonly PreparedTemplateKit _sources;
    readonly ITemplateLanguage _language;

    internal PreparedTemplates(PreparedTemplateKit sources, ITemplateLanguage language)
    {
        _sources = sources;
        _language = language;
    }

    /// <summary>Gets section names, in source order, by declared template path.</summary>
    public IReadOnlyDictionary<string, ImmutableArray<string>> Sections => _sources.SectionNames;

    /// <summary>Creates an isolated plan. The caller must discard all outputs on any refusal.</summary>
    /// <param name="budget">Positive aggregate plan limits.</param>
    /// <param name="types">Semantic identity lookup.</param>
    /// <returns>A disposable non-thread-safe plan session.</returns>
    public TemplatePlanSession BeginPlan(TemplateBudget budget, ITypeDirectory types) => new(_sources, budget, _language, types);
}
