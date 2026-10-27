// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Templates.for_TemplatePreparer;

public class when_preparing : given.a_kit
{
    TemplateResult<PreparedTemplateKit> _valid = null!;
    TemplateResult<PreparedTemplateKit> _oversized = null!;
    TemplateResult<PreparedTemplateKit> _nested = null!;
    TemplateResult<PreparedTemplateKit> _invalid = null!;
    TemplateResult<PreparedTemplateKit> _privateUse = null!;

    void Because()
    {
        _valid = TemplatePreparer.Prepare(Kit("{{ # if ( ( (\n 'if ( !' }}{%{ {{ if ((( }} }%}"));
        _oversized = TemplatePreparer.Prepare(Kit(new string('é', 32769)));
        _nested = TemplatePreparer.Prepare(Kit("{{ " + new string('!', 33) + "true }}"));
        _invalid = TemplatePreparer.Prepare(Kit("{{ if }}"));
        _privateUse = TemplatePreparer.Prepare(Kit("\U000F0000"));
    }

    [Fact] void should_ignore_strings_comments_and_escaped_raw_text() => _valid.Succeeded.ShouldBeTrue();
    [Fact] void should_limit_utf8_bytes() => _oversized.Diagnostics[0].Code.ShouldEqual(TemplateCodes.TemplateSize);
    [Fact] void should_refuse_nesting_before_parse() => _nested.Diagnostics[0].Code.ShouldEqual(TemplateCodes.Nesting);
    [Fact] void should_map_parse_errors() => _invalid.Diagnostics[0].Code.ShouldEqual(TemplateCodes.Parse);
    [Fact] void should_refuse_supplementary_private_use() => _privateUse.Diagnostics[0].Code.ShouldEqual(TemplateCodes.TemplatePrivateUse);
    [Fact] void should_not_expose_a_refused_preparation() => _invalid.Value.ShouldBeNull();
}
