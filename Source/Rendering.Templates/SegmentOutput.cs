// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text;
using Scriban.Runtime;

namespace Cratis.Stage.Rendering.Templates;

internal sealed class SegmentOutput(PlanMeter meter) : IScriptOutput
{
    readonly ImmutableArray<TemplateSegment>.Builder _segments = ImmutableArray.CreateBuilder<TemplateSegment>();
    readonly StringBuilder _text = new();
    readonly Encoder _encoder = Encoding.UTF8.GetEncoder();

    public void Write(string text, int offset, int count) => Append(text, offset, count);

    public ValueTask WriteAsync(string text, int offset, int count, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Append(text, offset, count);
        return ValueTask.CompletedTask;
    }

    internal void Symbol(TemplateSymbolSegment segment)
    {
        Flush();
        if (segment.Symbol is { } symbol)
        {
            if (string.IsNullOrEmpty(symbol.Namespace) || string.IsNullOrEmpty(symbol.Name) || TemplatePreScan.HasPrivateUse(symbol.Namespace) || TemplatePreScan.HasPrivateUse(symbol.Name)) throw JsonScriptValues.Invalid("Invalid symbol spelling.");
            meter.Bytes(Encoding.UTF8.GetByteCount(symbol.Namespace) + Encoding.UTF8.GetByteCount(symbol.Name));
        }
        _segments.Add(segment);
    }

    internal void Emit(ImmutableArray<TemplateSegment> segments)
    {
        foreach (var segment in segments)
        {
            if (segment is TemplateTextSegment text)
            {
                if (TemplatePreScan.HasPrivateUse(text.Text)) throw JsonScriptValues.Invalid("Language seam emitted private-use text.");
                Write(text.Text, 0, text.Text.Length);
            }
            else if (segment is TemplateSymbolSegment symbol)
            {
                Symbol(symbol);
            }
            else
            {
                throw JsonScriptValues.Invalid("Unknown language segment.");
            }
        }
    }

    internal ImmutableArray<TemplateSegment> Finish()
    {
        Flush();
        return _segments.ToImmutable();
    }

    void Append(string text, int offset, int count)
    {
        meter.Bytes(_encoder.GetByteCount(text.AsSpan(offset, count), false));
        _text.Append(text, offset, count);
    }

    void Flush()
    {
        meter.Bytes(_encoder.GetByteCount([], true));
        if (_text.Length == 0) return;
        _segments.Add(new TemplateTextSegment(_text.ToString()));
        _text.Clear();
    }
}
