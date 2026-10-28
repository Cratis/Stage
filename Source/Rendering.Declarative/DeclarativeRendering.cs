// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text;
using System.Text.Json;
using Cratis.Screenplay.Semantics;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Templates;

namespace Cratis.Stage.Rendering.Declarative;

/// <summary>
/// Represents one gap reported by a template, with the construct that owns it.
/// </summary>
/// <param name="Owner">The owning construct, or <see langword="null"/> when no construct could be attributed.</param>
/// <param name="Gap">The reported gap.</param>
/// <param name="Placeholder">The placeholder kind the gap role maps to.</param>
sealed record DeclarativeOwnedGap(SemanticId? Owner, TemplateGap Gap, ArtifactRenderPlaceholderKind Placeholder);

/// <summary>
/// Represents one rendered file before encoding.
/// </summary>
/// <param name="Path">The rendered relative path.</param>
/// <param name="Text">The resolved text.</param>
/// <param name="Item">The fan-out item the file belongs to, or <see langword="null"/> for a single output.</param>
/// <param name="Gaps">The gaps the template reported.</param>
sealed record DeclarativeRenderedFile(string Path, string Text, SemanticId? Item, ImmutableArray<DeclarativeOwnedGap> Gaps);

/// <summary>
/// Represents the rendering of every expanded output, or the safety refusal that stopped it.
/// </summary>
/// <param name="Files">The rendered files in expansion order.</param>
/// <param name="Refusal">The refusal, or <see langword="null"/> when rendered.</param>
sealed record DeclarativeRendering(ImmutableArray<DeclarativeRenderedFile> Files, ArtifactRenderDiagnostic? Refusal)
{
    const string SentinelPrefix = "/*__stage_";

    internal static DeclarativeRendering Render(TemplatePlanSession session, IDeclarativeLanguage language, ImmutableArray<ExpandedOutput> outputs, JsonElement root)
    {
        var renderer = new FileRenderer(session, language, Constructs(root));
        var files = new List<DeclarativeRenderedFile>();
        foreach (var expanded in outputs)
        {
            var refusal = renderer.Render(expanded, files);
            if (refusal is not null)
            {
                return new([], refusal);
            }
        }

        return new([.. files], null);
    }

    internal static ArtifactRenderPlaceholderKind Placeholder(GapRole role) => Enum.Parse<ArtifactRenderPlaceholderKind>(role.ToString());

    static ImmutableHashSet<SemanticId> Constructs(JsonElement root)
    {
        var ids = new HashSet<SemanticId>();
        if (root.TryGetProperty("items", out var items))
        {
            foreach (var kind in new[] { "commands", "reactions" })
            {
                AddIds(items, kind, ids);
            }
        }

        return [.. ids];
    }

    static void AddIds(JsonElement items, string kind, HashSet<SemanticId> ids)
    {
        if (!items.TryGetProperty(kind, out var list) || list.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var item in list.EnumerateArray())
        {
            if (item.TryGetProperty("id", out var id) && SemanticId.TryParse(id.GetString(), out var parsed))
            {
                ids.Add(parsed);
            }
        }
    }

    sealed class FileRenderer(TemplatePlanSession session, IDeclarativeLanguage language, ImmutableHashSet<SemanticId> constructs)
    {
        internal ArtifactRenderDiagnostic? Render(ExpandedOutput expanded, List<DeclarativeRenderedFile> files)
        {
            var path = session.RenderPath(expanded.Output.Id, expanded.Scope);
            if (path.Refusal is not null)
            {
                return DeclarativeOutputExpansion.Refuse(path.Refusal.Code, path.Refusal.Message);
            }

            var rendered = session.Render(expanded.Output.Template, expanded.Scope);
            if (rendered.Refusal is not null)
            {
                return DeclarativeOutputExpansion.Refuse(rendered.Refusal.Code, rendered.Refusal.Message);
            }

            var resolution = language.Resolve(path.Value!, [.. rendered.Output!.Segments.Select(Segment)]);
            if (resolution.Refusal is not null)
            {
                return DeclarativeOutputExpansion.Refuse(resolution.Refusal.Code, resolution.Refusal.Message);
            }

            if (resolution.Pieces.OfType<DeclarativeGeneratedPiece>().Any(_ => _.Text.Contains(SentinelPrefix, StringComparison.Ordinal)))
            {
                return DeclarativeOutputExpansion.Refuse(DeclarativeCodes.SentinelResidue, $"The generated text of {path.Value} contains sentinel-shaped residue.");
            }

            var item = expanded.ItemId is null ? (SemanticId?)null : SemanticId.Parse(expanded.ItemId);
            var gaps = rendered.Output.Gaps.Select(gap => new DeclarativeOwnedGap(item ?? Owner(gap), gap, Placeholder(gap.Role)));
            files.Add(new(path.Value!, Text(resolution.Pieces), item, [.. gaps]));
            return null;
        }

        static DeclarativeSegment Segment(TemplateSegment segment) => segment switch
        {
            TemplateTextSegment text => new DeclarativeTextSegment(text.Text),
            TemplateSymbolSegment symbol => new DeclarativeSymbolSegment(symbol.Kind, symbol.Symbol),
            _ => throw new InvalidOperationException("Unknown template segment.")
        };

        static string Text(ImmutableArray<DeclarativePiece> pieces)
        {
            var text = new StringBuilder();
            foreach (var piece in pieces)
            {
                text.Append(piece switch
                {
                    DeclarativeGeneratedPiece generated => generated.Text,
                    DeclarativeAuthoredPiece authored => authored.Text,
                    _ => throw new InvalidOperationException("Unknown piece.")
                });
            }

            return text.ToString();
        }

        SemanticId? Owner(TemplateGap gap) => SemanticId.TryParse(gap.ModelPath, out var id) && constructs.Contains(id) ? id : null;
    }
}
