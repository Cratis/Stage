// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticReactionSpecificationRenderer.given;

public class a_filtered_reaction : a_reaction_specification
{
    internal const string Definitions = """
        eventsource Account
          id "stored-account"
          identifier String
          stream Notes
            id "stored-notes"
            streamId Uuid
          stream Archive
            id "stored-archive"
            streamId Uuid
        """;

    internal static string FilteredSource(string filter = "Account.Notes", string? stream = "Notes") => Definitions + "\n" + ProductionSource
        .Replace("      reaction Worker", $"      reaction Worker\n        from {filter}", StringComparison.Ordinal)
        .Replace("        when append Started", stream is null ? "        when append Started" : $"        when append Started\n          for \"one\"\n          stream Account.{stream}\n            streamId = \"3fa85f64-5717-4562-b3fc-2c963f66afa6\"", StringComparison.Ordinal);

    void Establish() => Prepare(FilteredSource());
}
