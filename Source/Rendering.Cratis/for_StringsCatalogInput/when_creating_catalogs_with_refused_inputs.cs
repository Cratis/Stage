// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.Scaffolding;
using Cratis.Stage.Rendering.Cratis.Semantics;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_StringsCatalogInput;

public class when_creating_catalogs_with_refused_inputs : Specification
{
    const string Valid = "invoice.a = \"A\"\n";

    InvalidCratisBackendApplicationScaffold? _invalidDefault;
    InvalidCratisBackendApplicationScaffold? _noFiles;
    InvalidCratisBackendApplicationScaffold? _missingDefault;
    InvalidCratisBackendApplicationScaffold? _rooted;
    InvalidCratisBackendApplicationScaffold? _parent;
    InvalidCratisBackendApplicationScaffold? _backslash;
    InvalidCratisBackendApplicationScaffold? _extension;
    InvalidCratisBackendApplicationScaffold? _noLocale;
    InvalidCratisBackendApplicationScaffold? _badLocale;
    InvalidCratisBackendApplicationScaffold? _malformed;
    InvalidCratisBackendApplicationScaffold? _control;
    InvalidCratisBackendApplicationScaffold? _accepted;

    static InvalidCratisBackendApplicationScaffold? Refuse(Dictionary<string, string> files, string defaultLocale)
    {
        try
        {
            StringsCatalogInput.Create(files, defaultLocale);
            return null;
        }
        catch (InvalidCratisBackendApplicationScaffold error)
        {
            return error;
        }
    }

    void Because()
    {
        _invalidDefault = Refuse(new() { ["a.en.strings"] = Valid }, "1");
        _noFiles = Refuse([], "en");
        _missingDefault = Refuse(new() { ["a.en.strings"] = Valid }, "nb");
        _rooted = Refuse(new() { ["/a.en.strings"] = Valid }, "en");
        _parent = Refuse(new() { ["../a.en.strings"] = Valid }, "en");
        _backslash = Refuse(new() { ["x\\a.en.strings"] = Valid }, "en");
        _extension = Refuse(new() { ["a.en.txt"] = Valid }, "en");
        _noLocale = Refuse(new() { ["en.strings"] = Valid }, "en");
        _badLocale = Refuse(new() { ["a.1.strings"] = Valid }, "en");
        _malformed = Refuse(new() { ["a.en.strings"] = "not a strings line" }, "en");
        _control = Refuse(new() { ["a.en.strings"] = "invoice.a = \"A\u0001\"\n" }, "en");
        _accepted = Refuse(new() { ["nested/a.en.strings"] = Valid }, "en");
    }

    [Fact] void should_refuse_an_invalid_default_locale() => _invalidDefault.ShouldNotBeNull();
    [Fact] void should_refuse_no_files() => _noFiles.ShouldNotBeNull();
    [Fact] void should_refuse_a_missing_default_locale() => _missingDefault.ShouldNotBeNull();
    [Fact] void should_refuse_a_rooted_path() => _rooted.ShouldNotBeNull();
    [Fact] void should_refuse_a_parent_segment() => _parent.ShouldNotBeNull();
    [Fact] void should_refuse_a_backslash() => _backslash.ShouldNotBeNull();
    [Fact] void should_refuse_a_wrong_extension() => _extension.ShouldNotBeNull();
    [Fact] void should_refuse_a_missing_locale_segment() => _noLocale.ShouldNotBeNull();
    [Fact] void should_refuse_an_invalid_locale_segment() => _badLocale.ShouldNotBeNull();
    [Fact] void should_refuse_a_malformed_line() => _malformed.ShouldNotBeNull();
    [Fact] void should_refuse_a_control_character_value() => _control.ShouldNotBeNull();
    [Fact] void should_accept_a_nested_valid_path() => _accepted.ShouldBeNull();
}
