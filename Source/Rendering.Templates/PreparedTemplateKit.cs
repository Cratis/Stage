// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Stage.Rendering.Kits;
using Scriban;

namespace Cratis.Stage.Rendering.Templates;

/// <summary>An immutable parsed kit, shareable between independent plan sessions.</summary>
internal sealed class PreparedTemplateKit
{
    internal PreparedTemplateKit(LoadedKit kit, ImmutableDictionary<string, Template> templates, ImmutableDictionary<string, Template> expressions, OperatorTable? operators = null)
    {
        Kit = kit;
        Templates = templates;
        Expressions = expressions;
        Operators = operators ?? OperatorTable.Empty;
    }

    internal LoadedKit Kit { get; }

    internal OperatorTable Operators { get; }

    internal ImmutableDictionary<string, Template> Templates { get; }

    internal ImmutableDictionary<string, Template> Expressions { get; }

    internal TemplatePlanSession BeginPlan(TemplateBudget budget) => new(this, budget);
}
