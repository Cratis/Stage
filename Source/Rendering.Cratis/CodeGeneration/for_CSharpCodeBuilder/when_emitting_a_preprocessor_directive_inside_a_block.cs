// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Xunit;

namespace Cratis.Stage.Rendering.Cratis.CodeGeneration.for_CSharpCodeBuilder;

public class when_emitting_a_preprocessor_directive_inside_a_block
{
    readonly string[] _lines = new CSharpCodeBuilder()
        .Namespace("Shop.Policies")
        .OpenBlock("public class Policy")
        .Line("#pragma warning disable CA2255 // Registered at module load.")
        .OpenBlock("internal static void Initialize()")
        .Line("# nullable enable")
        .Line("var tag = \"#pragma\";")
        .Line("#hashtag is not a directive")
        .EndBlock()
        .Line("    #pragma warning restore CA2255")
        .EndBlock()
        .ToString()
        .ReplaceLineEndings("\n")
        .Split('\n');

    [Fact] void should_emit_a_directive_in_a_type_at_column_zero() => Assert.Contains("#pragma warning disable CA2255 // Registered at module load.", _lines);
    [Fact] void should_emit_a_directive_in_a_nested_block_at_column_zero() => Assert.Contains("# nullable enable", _lines);
    [Fact] void should_emit_an_indented_directive_at_column_zero() => Assert.Contains("#pragma warning restore CA2255", _lines);
    [Fact] void should_keep_code_mentioning_a_directive_at_the_block_indent() => Assert.Contains("        var tag = \"#pragma\";", _lines);
    [Fact] void should_keep_a_line_that_is_not_a_directive_at_the_block_indent() => Assert.Contains("        #hashtag is not a directive", _lines);
}
