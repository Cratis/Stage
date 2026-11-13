// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Rendering;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_StringsCatalogInput;

public class when_reading_refused_catalog_inputs : Specification
{
    bool _wrongName;
    bool _notJson;
    bool _missingDefault;
    bool _badKey;
    bool _controlValue;
    bool _nonCanonical;
    bool _caseCollision;
    bool _canonical;

    static bool Read(string name, string json) => StringsCatalogInput.TryRead(
        ArtifactRenderInput.Create(name, StringsCatalogInput.Version, [.. Encoding.UTF8.GetBytes(json)]),
        out _);

    void Because()
    {
        _wrongName = Read("other", "{\"DefaultLocale\":\"en\",\"Locales\":{\"en\":{\"a\":\"A\"}}}");
        _notJson = Read(StringsCatalogInput.Name, "not json");
        _missingDefault = Read(StringsCatalogInput.Name, "{\"DefaultLocale\":\"nb\",\"Locales\":{\"en\":{\"a\":\"A\"}}}");
        _badKey = Read(StringsCatalogInput.Name, "{\"DefaultLocale\":\"en\",\"Locales\":{\"en\":{\"bad key\":\"A\"}}}");
        _controlValue = Read(StringsCatalogInput.Name, "{\"DefaultLocale\":\"en\",\"Locales\":{\"en\":{\"a\":\"A\\u0001\"}}}");
        _nonCanonical = Read(StringsCatalogInput.Name, "{\"DefaultLocale\":\"en\",\"Locales\":{\"en\":{\"b\":\"B\",\"a\":\"A\"}}}");
        _caseCollision = Read(StringsCatalogInput.Name, "{\"DefaultLocale\":\"en\",\"Locales\":{\"en\":{\"a\":\"A\"},\"EN\":{\"a\":\"A\"}}}");
        _canonical = Read(StringsCatalogInput.Name, "{\"DefaultLocale\":\"en\",\"Locales\":{\"en\":{\"a\":\"A\"}}}");
    }

    [Fact] void should_refuse_a_wrong_input_name() => _wrongName.ShouldBeFalse();
    [Fact] void should_refuse_bytes_that_are_not_json() => _notJson.ShouldBeFalse();
    [Fact] void should_refuse_a_missing_default_locale() => _missingDefault.ShouldBeFalse();
    [Fact] void should_refuse_an_invalid_key() => _badKey.ShouldBeFalse();
    [Fact] void should_refuse_a_control_character_value() => _controlValue.ShouldBeFalse();
    [Fact] void should_refuse_a_non_canonical_encoding() => _nonCanonical.ShouldBeFalse();
    [Fact] void should_refuse_case_colliding_locales() => _caseCollision.ShouldBeFalse();
    [Fact] void should_accept_the_canonical_encoding() => _canonical.ShouldBeTrue();
}
