// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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

    /// <summary>Creates an isolated plan. The caller must discard all outputs on any refusal.</summary>
    /// <param name="budget">Positive aggregate plan limits.</param>
    /// <returns>A disposable non-thread-safe plan session.</returns>
    public TemplatePlanSession BeginPlan(TemplateBudget budget) => new(_sources, budget, _language);
}
