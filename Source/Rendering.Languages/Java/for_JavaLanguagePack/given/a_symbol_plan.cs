// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaLanguagePack.given;

public class a_symbol_plan : Specification
{
    protected static readonly TypeName KnownType = new("example", "Outer", ["Inner"]);

    protected static SymbolPlan WithSegment(SymbolSegment segment) => WithSegments(segment);

    protected static SymbolPlan WithSegments(params SymbolSegment[] segments) => new([new("file", "example", segments)], []);

    protected static IEnumerable<SymbolPlan> WithEachField(string value)
    {
        yield return new([new(value, "example", [])], []);
        yield return new([new("file", value, [])], []);
        foreach (var type in TypeVariants(value))
        {
            yield return new([], [type]);
            yield return WithSegment(new DeclarationSegment(type, "owner"));
            yield return WithSegment(new NameSegment(type));
            yield return WithSegment(new ReferenceSegment(type));
            yield return WithSegment(new NativeSegment("java", "body", "owner", "member", [type]));
        }

        foreach (var segment in SegmentsWithField(value)) yield return WithSegment(segment);
    }

    static TypeName[] TypeVariants(string value) =>
        [KnownType with { Package = value }, KnownType with { Name = value }, KnownType with { Nested = [value] }];

    static SymbolSegment[] SegmentsWithField(string value) =>
    [
        new TextSegment(value),
        new DeclarationSegment(KnownType, value),
        new DeclarationSegment(KnownType, "owner", value),
        new DeclarationSegment(KnownType, "owner", "scope", value),
        new NameSegment(KnownType, value),
        new ReferenceSegment(KnownType, value),
        new ReferenceSegment(KnownType, "owner", value),
        new NativeSegment(value, "body", "owner", "member", []),
        new NativeSegment("java", value, "owner", "member", []),
        new NativeSegment("java", "body", value, "member", []),
        new NativeSegment("java", "body", "owner", value, []),
        new GapSegment(value, "construct", "intent", "member"),
        new GapSegment("existing-code", value, "intent", "member"),
        new GapSegment("existing-code", "construct", value, "member"),
        new GapSegment("existing-code", "construct", "intent", value),
    ];
}
