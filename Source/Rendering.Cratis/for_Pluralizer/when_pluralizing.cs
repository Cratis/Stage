// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.Naming;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_Pluralizer;

public class when_pluralizing : Specification
{
    string _regular = null!;
    string _endingInYAfterConsonant = null!;
    string _endingInYAfterVowel = null!;
    string _endingInS = null!;
    string _endingInCh = null!;

    void Because()
    {
        _regular = Pluralizer.Pluralize("Author");
        _endingInYAfterConsonant = Pluralizer.Pluralize("Category");
        _endingInYAfterVowel = Pluralizer.Pluralize("Day");
        _endingInS = Pluralizer.Pluralize("Status");
        _endingInCh = Pluralizer.Pluralize("Batch");
    }

    [Fact] void should_add_s_for_a_regular_word() => _regular.ShouldEqual("Authors");
    [Fact] void should_replace_y_with_ies_after_a_consonant() => _endingInYAfterConsonant.ShouldEqual("Categories");
    [Fact] void should_add_s_for_y_after_a_vowel() => _endingInYAfterVowel.ShouldEqual("Days");
    [Fact] void should_add_es_for_a_word_ending_in_s() => _endingInS.ShouldEqual("Statuses");
    [Fact] void should_add_es_for_a_word_ending_in_ch() => _endingInCh.ShouldEqual("Batches");
}
