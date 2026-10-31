// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Rendering.Cratis.CodeGeneration;
using Cratis.Stage.Rendering.Cratis.Naming;

namespace Cratis.Stage.Rendering.Cratis.Semantics;

/// <summary>
/// Emits parameter and return documentation alongside generated member declarations.
/// </summary>
internal static class SemanticMemberDocumentation
{
    internal static CSharpCodeBuilder Parameters(this CSharpCodeBuilder builder, IEnumerable<SemanticProperty> properties, SemanticApplicationContext? context = null)
    {
        foreach (var property in properties)
        {
            builder.Parameter(context?.MemberNames.Property(property) ?? Identifiers.ToPascalCase(property.Name), $"The {Identifiers.ToWords(property.Name)}.");
        }

        return builder;
    }

    internal static CSharpCodeBuilder Parameter(this CSharpCodeBuilder builder, string name, string description) =>
        builder.Line($"/// <param name=\"{SecurityElement.Escape(name.TrimStart('@'))}\">{SecurityElement.Escape(description)}</param>");

    internal static CSharpCodeBuilder Returns(this CSharpCodeBuilder builder, string description) =>
        builder.Line($"/// <returns>{SecurityElement.Escape(description)}</returns>");
}
