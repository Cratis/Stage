// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaTypeFacts;

public class when_reading_bundled_facts : Specification
{
    JavaTypeFacts _facts = null!;

    void Because() => _facts = JavaTypeFacts.Bundled;

    [Fact] void should_know_every_java_lang_type() => JavaLangTypes.Names.ShouldEachConformTo(name => _facts.TryGet("java.lang." + name, out _));
    [Fact] void should_include_threads_nested_deep_names_and_fields() => (_facts.TryGet("java.lang.Thread", out var names) && names.SetEquals(["Builder", "OfPlatform", "OfVirtual", "State", "UncaughtExceptionHandler", "MAX_PRIORITY", "MIN_PRIORITY", "NORM_PRIORITY"])).ShouldBeTrue();
    [Fact] void should_keep_jdk_21_string_template_names() => (_facts.TryGet("java.lang.StringTemplate", out var names) && names.SetEquals(["Processor", "Linkage", "RAW", "STR"])).ShouldBeTrue();
    [Fact] void should_include_deep_names_of_an_extensible_member_of_a_final_type() => (_facts.TryGet("java.lang.ProcessBuilder", out var names) && names.SetEquals(["Type", "PIPE", "INHERIT", "DISCARD"])).ShouldBeTrue();
    [Fact] void should_include_deep_names_of_inherited_member_types() => (_facts.TryGet("java.lang.Class", out var names) && names.SetEquals(["OfField", "OfMethod"])).ShouldBeTrue();
    [Fact] void should_not_inherit_final_character_constant_fields() => (_facts.TryGet("java.lang.Character", out var names) && names.IsEmpty).ShouldBeTrue();
    [Fact] void should_keep_big_decimals_jdk_union() => (_facts.TryGet("java.math.BigDecimal", out var names) && names.SetEquals(["ZERO", "ONE", "TWO", "TEN", "ROUND_UP", "ROUND_DOWN", "ROUND_CEILING", "ROUND_FLOOR", "ROUND_HALF_UP", "ROUND_HALF_DOWN", "ROUND_HALF_EVEN", "ROUND_UNNECESSARY"])).ShouldBeTrue();
    [Fact] void should_know_the_common_jdk_types() => new[] { "java.lang.annotation.Annotation", "java.util.UUID", "java.util.List", "java.time.LocalDate", "java.time.OffsetDateTime" }.ShouldEachConformTo(type => _facts.TryGet(type, out var names) && names.IsEmpty);
}
