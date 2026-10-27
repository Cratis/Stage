// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaFileScope;

public class when_no_spelling_has_a_proven_binding : given.a_file_scope
{
    Exception _error = null!;

    void Establish()
    {
        _file = _file with { Segments = [.. _file.Segments, new NativeSegment("java", "int com; class Widget {}", "native", "member", [])] };
        CreateScope(new TypeName("com.library", "Widget", []));
    }

    void Because() => _error = Catch.Exception(() => _scope.Spell(new("com.library", "Widget", [])));

    [Fact] void should_refuse_rather_than_emit_a_misbound_reference() => ((LanguagePackRefusal)_error).Code.ShouldEqual(JavaRefusalCodes.ObscuredReference);
}
