// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Templates.for_TemplatePreparer;

public class when_linting : given.a_kit
{
    [Theory]
    [InlineData("{{ date.now }}")]
    [InlineData("{{ string.base64_decode 'x' }}")]
    [InlineData("{{ x = string }}")]
    [InlineData("{{ x = @string.upcase }}")]
    [InlineData("{{ string['upcase'] 'x' }}")]
    [InlineData("{{ import model }}")]
    [InlineData("{{ with model }}x{{ end }}")]
    [InlineData("{{ missing }}")]
    public void should_refuse_non_allowlisted_access(string text) => TemplatePreparer.Prepare(Kit(text)).Diagnostics[0].Code.ShouldEqual(TemplateCodes.Allowlist);

    [Theory]
    [InlineData("{{ model.x = 1 }}")]
    [InlineData("{{ string = {} }}")]
    [InlineData("{{ item = 0 }}")]
    public void should_refuse_root_and_model_mutation(string text) => TemplatePreparer.Prepare(Kit(text)).Diagnostics[0].Code.ShouldEqual(TemplateCodes.Mutation);

    [Theory]
    [InlineData("{{ render model.partial }}")]
    [InlineData("{{ render 'missing' }}")]
    public void should_require_a_declared_literal_partial(string text) => TemplatePreparer.Prepare(Kit(text)).Diagnostics[0].Code.ShouldEqual(TemplateCodes.Partial);

    [Fact] public void should_keep_bad_expressions_even_if_the_output_would_be_skipped() => TemplatePreparer.Prepare(Kit(when: "date.now")).Diagnostics[0].Code.ShouldEqual(TemplateCodes.Allowlist);
    [Fact] public void should_allow_local_decisions_and_ordinal_builtins() => TemplatePreparer.Prepare(Kit("{{ x = 'i'; if x; string.upcase x; end }}")).Succeeded.ShouldBeTrue();
}
