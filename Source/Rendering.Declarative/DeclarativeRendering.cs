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

    internal static DeclarativeRendering Render(TemplatePlanSession session, IDeclarativeLanguage language, ImmutableArray<ExpandedOutput> outputs, JsonElement root, ArtifactRenderRequest request, long maxOutputBytes)
    {
        var renderer = new FileRenderer(session, language, Constructs(root), new(request, language, session), maxOutputBytes);
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

    sealed class FileRenderer(TemplatePlanSession session, IDeclarativeLanguage language, ImmutableHashSet<SemanticId> constructs, DeclarativeBodies bodies, long maxOutputBytes)
    {
        readonly HashSet<string> _paths = new(StringComparer.OrdinalIgnoreCase);
        long _finalBytes;

        internal ArtifactRenderDiagnostic? Render(ExpandedOutput expanded, List<DeclarativeRenderedFile> files)
        {
            var path = session.RenderPath(expanded.Output.Id, expanded.Scope);
            if (path.Refusal is not null)
            {
                return DeclarativeOutputExpansion.Refuse(path.Refusal.Code, path.Refusal.Message);
            }

            var invalid = DeclarativePaths.Check(path.Value!, _paths);
            if (invalid is not null)
            {
                return invalid;
            }

            var rendered = session.Render(expanded.Output.Template, expanded.Scope);
            if (rendered.Refusal is not null)
            {
                return DeclarativeOutputExpansion.Refuse(rendered.Refusal.Code, rendered.Refusal.Message);
            }

            var item = expanded.ItemId is null ? (SemanticId?)null : SemanticId.Parse(expanded.ItemId);
            var mapped = Map(rendered.Output!.Segments, item, expanded.Output.Template);
            if (mapped.Refusal is not null)
            {
                return mapped.Refusal;
            }

            var resolution = language.Resolve(path.Value!, mapped.Segments);
            if (resolution.Refusal is not null)
            {
                return DeclarativeOutputExpansion.Refuse(resolution.Refusal.Code, resolution.Refusal.Message);
            }

            if (GeneratedRuns(resolution.Pieces).Any(_ => _.Contains(SentinelPrefix, StringComparison.Ordinal)))
            {
                return DeclarativeOutputExpansion.Refuse(DeclarativeCodes.SentinelResidue, $"The generated text of {path.Value} contains sentinel-shaped residue.");
            }

            var text = Text(resolution.Pieces);
            if (text.StartsWith('\uFEFF'))
            {
                return DeclarativeOutputExpansion.Refuse(DeclarativeCodes.OutputText, $"The text of {path.Value} starts with a byte order mark, which a text artifact cannot carry.");
            }

            // The template session meters what templates emit; language resolution (for example import blocks) adds text it never sees, so the final text of every file is also capped, in aggregate, by the same limit.
            _finalBytes += Encoding.UTF8.GetByteCount(text);
            if (_finalBytes > maxOutputBytes)
            {
                return DeclarativeOutputExpansion.Refuse(TemplateCodes.Output, "Plan UTF-8 output budget exceeded.");
            }

            var gaps = rendered.Output.Gaps.Select(gap => new DeclarativeOwnedGap(item ?? Owner(gap), gap, Placeholder(gap.Role))).Concat(mapped.Gaps);
            files.Add(new(path.Value!, text, item, [.. gaps]));
            return null;
        }

        // Authored pieces are never scanned, so a marker straddling a generated|authored boundary is deliberately not detected: authored bytes may legitimately contain anything and end a run.
        static IEnumerable<string> GeneratedRuns(ImmutableArray<DeclarativePiece> pieces)
        {
            var run = new StringBuilder();
            foreach (var piece in pieces)
            {
                if (piece is DeclarativeGeneratedPiece generated)
                {
                    run.Append(generated.Text);
                    continue;
                }

                if (run.Length > 0)
                {
                    yield return run.ToString();
                    run.Clear();
                }
            }

            if (run.Length > 0)
            {
                yield return run.ToString();
            }
        }

        static string Lf(StringBuilder run) => run.ToString().Replace("\r\n", "\n", StringComparison.Ordinal).Replace((char)13, (char)10);

        // Generated runs are normalized to LF; authored pieces are appended byte for byte. A run ends at an authored piece, so a CR at the end of a run is never joined to an authored LF.
        static string Text(ImmutableArray<DeclarativePiece> pieces)
        {
            var text = new StringBuilder();
            var run = new StringBuilder();
            foreach (var piece in pieces)
            {
                switch (piece)
                {
                    case DeclarativeGeneratedPiece generated:
                        run.Append(generated.Text);
                        break;
                    case DeclarativeAuthoredPiece authored:
                        text.Append(Lf(run)).Append(authored.Text);
                        run.Clear();
                        break;
                    default:
                        throw new InvalidOperationException("Unknown piece.");
                }
            }

            return text.Append(Lf(run)).ToString();
        }

        SemanticId? Owner(TemplateGap gap) => SemanticId.TryParse(gap.ModelPath, out var id) && constructs.Contains(id) ? id : null;

        (ImmutableArray<DeclarativeSegment> Segments, ImmutableArray<DeclarativeOwnedGap> Gaps, ArtifactRenderDiagnostic? Refusal) Map(ImmutableArray<TemplateSegment> segments, SemanticId? item, string template)
        {
            var mapped = new List<DeclarativeSegment>();
            var gaps = new List<DeclarativeOwnedGap>();
            foreach (var segment in segments)
            {
                switch (segment)
                {
                    case TemplateTextSegment text:
                        mapped.Add(new DeclarativeTextSegment(text.Text));
                        break;
                    case TemplateSymbolSegment symbol:
                        mapped.Add(new DeclarativeSymbolSegment(symbol.Kind, symbol.Symbol));
                        break;
                    case TemplateAuthoredSegment authored:
                        var body = bodies.Resolve(authored.RequirementId, item, template);
                        if (body.Refusal is not null)
                        {
                            return (default, default, body.Refusal);
                        }

                        mapped.Add(body.Segment!);
                        if (body.Gap is not null)
                        {
                            gaps.Add(body.Gap);
                        }

                        break;
                    default:
                        throw new InvalidOperationException("Unknown template segment.");
                }
            }

            ImmutableArray<DeclarativeSegment> segmentsArray = [.. mapped];
            ImmutableArray<DeclarativeOwnedGap> gapsArray = [.. gaps];
            return (segmentsArray, gapsArray, null);
        }
    }
}
