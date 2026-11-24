// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text;
using Cratis.Stage.Contracts.Rendering;

namespace Cratis.Stage.Rendering.Cratis.for_CratisBackendApplicationScaffold.given;

/// <summary>Restores only Program.cs's pre-Arc#3975 availability registration placement for historical freezes.</summary>
internal static class AvailabilityRegistrationOrder
{
    internal static ImmutableArray<ArtifactRenderInput> Restore(ImmutableArray<ArtifactRenderInput> inputs, string rootNamespace) =>
        [.. inputs.Select(input => input.Name == "cratis-scaffold:text:Program.cs"
            ? CratisArtifactRenderInput.CreateText("Program.cs", input.Version, RestoreProgram(Encoding.UTF8.GetString(input.Bytes.AsSpan()), rootNamespace))
            : input)];

    static string RestoreProgram(string text, string rootNamespace)
    {
        var registration = $"{rootNamespace}.GeneratedAvailability.Registration.Register(builder);\n";
        var early = "// Register first to preserve availability options and scoped request context (Cratis/Arc#3975).\n" + registration + "\n";
        var provides = $"{rootNamespace}.GeneratedProvides.Registration.Register(builder.Services);\n";
        RequireOne(text, early);
        RequireOne(text, provides);
        return text.Replace(early, string.Empty, StringComparison.Ordinal)
            .Replace(provides, provides + registration, StringComparison.Ordinal);
    }

    static void RequireOne(string text, string fragment)
    {
        if (text.Split(fragment, StringSplitOptions.None).Length != 2)
        {
            throw new InvalidOperationException("The frozen Program.cs no longer contains the exact availability-order change.");
        }
    }
}
