// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.CodeGeneration.for_StreamIdsRendering;

public class when_differing_from_the_screenplay_formatter : given.a_compiled_codec
{
    readonly List<string> _differences = [];

    void Because()
    {
        foreach (var text in new[] { "", "text", "a|b%", "é", "e\u0301", "日本\U0001f600", "\ud800", "\udc00", " \t" })
        {
            var actual = Text(text, out var formatted);
            var expected = SemanticStreamIdFormatter.TryFormatText(text, out var canonical, out _);
            if (actual != expected || (actual && formatted != canonical)) _differences.Add("Text");
        }
        foreach (var integer in new[] { int.MinValue, -42, 0, 42, int.MaxValue })
        {
            SemanticStreamIdFormatter.TryFormatInteger(integer, true, out var canonical, out _);
            if ((string)Invoke("Integer", integer) != canonical) _differences.Add("Integer");
        }
        var uuid = Guid.Parse("3FA85F64-5717-4562-B3FC-2C963F66AFA6");
        if ((string)Invoke("Uuid", uuid) != SemanticStreamIdFormatter.FormatUuid(uuid)) _differences.Add("Uuid");
        string[] parts = ["a|b%", "%7C", "é"];
        if ((string)Invoke("Composite", (object)parts) != SemanticStreamIdFormatter.EncodeComposite(parts)) _differences.Add("Composite");
    }

    [Fact] void should_have_no_formatter_differences() => _differences.ShouldBeEmpty();
}
