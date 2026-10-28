// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text;
using Cratis.Stage.Rendering.Declarative;
using Cratis.Stage.Rendering.Templates;

namespace Cratis.Stage.Rendering.Languages.Java;

/// <summary>
/// Resolves every file of a validated plan into ordered pieces, attaching each refusal to the file that causes it.
/// </summary>
static class JavaResolution
{
    internal static ImmutableArray<DeclarativeFileResolution> Resolve(JavaPlan plan, JavaTypeFacts facts)
    {
        var analyses = new Dictionary<string, JavaFileAnalysis>(StringComparer.Ordinal);
        var refusals = new Dictionary<string, LanguagePackRefusal>(StringComparer.Ordinal);
        foreach (var file in plan.Files.Where(file => file.Path.EndsWith(".java", StringComparison.Ordinal)))
        {
            try
            {
                analyses[file.Path] = JavaFileAnalysis.Create(file, plan);
            }
            catch (LanguagePackRefusal refusal)
            {
                refusals[file.Path] = refusal;
            }
        }

        var closure = JavaClosure.Compute(plan, analyses.Values, facts);
        var packageTypes = plan.Declarations.Keys.Where(symbol => !symbol.Name.Contains('.', StringComparison.Ordinal))
            .GroupBy(symbol => symbol.Namespace, StringComparer.Ordinal)
            .ToImmutableDictionary(group => group.Key, group => group.Select(symbol => symbol.Name).ToImmutableHashSet(StringComparer.Ordinal), StringComparer.Ordinal);
        var context = new Resolving(plan, analyses, refusals, closure, packageTypes);
        return [.. plan.Files.Select(file => ResolveFile(file, context))];
    }

    static DeclarativeFileResolution ResolveFile(DeclarativeLanguageFile file, Resolving context)
    {
        try
        {
            if (context.Refusals.TryGetValue(file.Path, out var early) || context.Closure.Refusals.TryGetValue(file.Path, out early))
            {
                throw early;
            }

            return DeclarativeFileResolution.Resolved(context.Analyses.TryGetValue(file.Path, out var analysis)
                ? EmitJava(context, analysis)
                : EmitCanonical(context.Plan, file));
        }
        catch (LanguagePackRefusal refusal)
        {
            return DeclarativeFileResolution.Refused(refusal.Code, refusal.Message);
        }
    }

    static ImmutableArray<DeclarativePiece> EmitJava(Resolving context, JavaFileAnalysis analysis)
    {
        var plan = context.Plan;
        var path = analysis.File.Path;
        var spelling = JavaSpelling.Compute(
            analysis,
            context.Closure.Closures[path],
            context.Closure.Introduced[path],
            context.PackageTypes.GetValueOrDefault(analysis.Package, []));
        var emitter = new PieceEmitter();
        var declared = new HashSet<TemplateSymbol>();
        foreach (var segment in analysis.File.Segments)
        {
            switch (segment)
            {
                case DeclarativeSymbolSegment { Kind: TemplateSymbolKind.Imports }:
                    emitter.AppendText(JavaImportBlock.Render(analysis.Package, spelling.Imports));
                    break;
                case DeclarativeSymbolSegment { Kind: TemplateSymbolKind.Declaration } declaration:
                    var leaf = JavaFileAnalysis.Leaf(plan.Declarations[declaration.Value!].Allocated.Name);
                    if (!declared.Add(declaration.Value!))
                    {
                        RejectCapturedRepeat(analysis, context.Closure.Closures[path], spelling, declaration.Value!, leaf);
                    }

                    emitter.AppendSpelling(leaf);
                    break;
                case DeclarativeSymbolSegment reference:
                    emitter.AppendSpelling(spelling.Heads[JavaFileAnalysis.Head(reference.Value!)] + Remainder(plan, reference.Value!));
                    break;
                default:
                    emitter.Append(segment);
                    break;
            }
        }

        return emitter.Finish();
    }

    // A repeated Declaration is normally a constructor name, but a kit may use it as a type. Written bare it would bind to an inherited
    // member type (or another type claimed short) of the same name, so such a repeat is refused. The declaration's own nested name is always in the
    // closure and is not a capture.
    static void RejectCapturedRepeat(JavaFileAnalysis analysis, ImmutableHashSet<string> closure, JavaSpelling spelling, TemplateSymbol declaration, string leaf)
    {
        var ownHead = JavaFileAnalysis.Head(declaration);
        var claimed = spelling.Heads.Any(entry => entry.Key != ownHead && entry.Key.Name == leaf && entry.Value == leaf);
        if (claimed || (closure.Contains(leaf) && !analysis.NestedDeclarations.Contains(leaf)))
        {
            throw new LanguagePackRefusal(JavaRefusalCodes.Unspellable, $"The repeated declaration '{leaf}' in '{analysis.File.Path}' would bind to an inherited or imported name.");
        }
    }

    static ImmutableArray<DeclarativePiece> EmitCanonical(JavaPlan plan, DeclarativeLanguageFile file)
    {
        var emitter = new PieceEmitter();
        foreach (var segment in file.Segments)
        {
            if (segment is DeclarativeSymbolSegment reference)
            {
                emitter.AppendText(reference.Value!.Namespace + "." + Allocated(plan, reference.Value!));
            }
            else
            {
                emitter.Append(segment);
            }
        }

        return emitter.Finish(java: false);
    }

    static string Allocated(JavaPlan plan, TemplateSymbol symbol) => plan.Declarations.TryGetValue(symbol, out var declaration) ? declaration.Allocated.Name : symbol.Name;

    static string Remainder(JavaPlan plan, TemplateSymbol symbol)
    {
        var name = Allocated(plan, symbol);
        var dot = name.IndexOf('.', StringComparison.Ordinal);
        return dot < 0 ? string.Empty : name[dot..];
    }

    sealed record Resolving(
        JavaPlan Plan,
        Dictionary<string, JavaFileAnalysis> Analyses,
        Dictionary<string, LanguagePackRefusal> Refusals,
        JavaClosure Closure,
        ImmutableDictionary<string, ImmutableHashSet<string>> PackageTypes);

    sealed class PieceEmitter
    {
        readonly List<DeclarativePiece> _pieces = [];
        readonly List<(int Start, int End)> _spans = [];
        readonly StringBuilder _text = new();
        readonly StringBuilder _generated = new();

        internal void Append(DeclarativeSegment segment)
        {
            if (segment is DeclarativeAuthoredSegment authored)
            {
                Flush();
                _pieces.Add(new DeclarativeAuthoredPiece(authored.Body));
                _text.Append(authored.Body);
            }
            else if (segment is DeclarativeTextSegment text)
            {
                AppendText(text.Value);
            }
        }

        internal void AppendText(string text)
        {
            _generated.Append(text);
            _text.Append(text);
        }

        internal void AppendSpelling(string spelling)
        {
            _spans.Add((_text.Length, _text.Length + spelling.Length));
            AppendText(spelling);
        }

        internal ImmutableArray<DeclarativePiece> Finish(bool java = true)
        {
            Flush();
            var full = _text.ToString();
            if (java)
            {
                JavaSpellingContext.Validate(full, _spans);
            }

            if (java)
            {
                JavaOutputIntegrity.Validate(full);
            }
            else
            {
                PrivateUseCodePoints.ValidateRaw(full);
            }

            return [.. _pieces];
        }

        void Flush()
        {
            if (_generated.Length > 0)
            {
                _pieces.Add(new DeclarativeGeneratedPiece(_generated.ToString()));
                _generated.Clear();
            }
        }
    }
}
