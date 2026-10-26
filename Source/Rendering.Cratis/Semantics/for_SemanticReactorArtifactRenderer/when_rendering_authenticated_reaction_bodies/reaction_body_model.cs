// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Stage.Contracts.Semantics;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticReactorArtifactRenderer.when_rendering_authenticated_reaction_bodies;

internal static class reaction_body_model
{
    internal const string Body = "return Title == @event.Title && context is not null ? new object[] { Title } : null;";

    internal static string Source(string body, bool file = false, string extraProperty = "", string extraTriggerContent = "") => $$"""
        module Support
          feature Tickets
            slice Automation Notify
              event TicketOpened
                title String
        {{extraProperty}}
              reaction Notifier
                when TicketOpened
        {{extraTriggerContent}}
        {{(file ? "          file Body.cs" : "          ```csharp\n" + string.Join('\n', body.Split('\n').Select(line => "          " + line)) + "\n          ```")}}
        """;

    internal static LoadedSemanticModel Load(string body = Body, bool file = false, string extraProperty = "", string extraTriggerContent = "")
    {
        return LoadSource(Source(body, file, extraProperty, extraTriggerContent), file ? body : null);
    }

    internal static LoadedSemanticModel LoadSource(string source, string? attachment = null)
    {
        var folder = Path.Combine(Path.GetTempPath(), "stage-reaction-bodies", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            File.WriteAllText(Path.Combine(folder, "Reaction.play"), source);
            if (attachment is not null) File.WriteAllText(Path.Combine(folder, "Body.cs"), attachment);
            return SemanticModelLoader.LoadFromPathAsync(folder, null, "ReactionModel").GetAwaiter().GetResult();
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }
}
#endif
