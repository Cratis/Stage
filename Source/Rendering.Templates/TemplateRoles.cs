// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Templates;

internal static class TemplateRoles
{
    internal static readonly string[] Names = ["throwing_operation_body", "deny_enclosing_operation", "disabled_specification", "unregistered_consumer", "retained_signature_or_type", "graph_proven_omitted_leaf"];

    internal static bool TryParse(string text, out GapRole role)
    {
        var index = Array.IndexOf(Names, text);
        role = (GapRole)index;
        return index >= 0;
    }

    internal static GapRole Parse(string text) => TryParse(text, out var role) ? role : throw JsonScriptValues.Invalid("Unknown gap role.");
}
