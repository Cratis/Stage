// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Templates;

internal static class plans
{
    internal static TemplatePlanSession BeginPlan(this PreparedTemplateKit prepared, TemplateBudget budget) => new(prepared, budget, new for_TemplateProfile.given.a_language(), new for_TemplateProfile.given.a_language());

    internal static TemplatePlanSession BeginPlan(this PreparedTemplates prepared, TemplateBudget budget) => prepared.BeginPlan(budget, new for_TemplateProfile.given.a_language());
}
