// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_CommandValidatorRenderer;

public class when_finalizing_the_external_base : an_application_with_external_bases
{
    async Task Because() => await _renderer.Render([_application], _targetDirectory, _output, _error);

    [Fact] void should_emit_the_short_base() => Artifact("/Register.cs").ShouldContain("    public class RegisterInvoiceValidator : CommandValidator<RegisterInvoice>");
    [Fact] void should_use_the_scaffold_global_import_for_the_base() => Artifact("/Register.cs").ShouldNotContain("using Cratis.Arc.Commands;");
    [Fact] void should_not_leave_the_base_qualified() => Artifact("/Register.cs").ShouldNotContain("global::Cratis.Arc.Commands.CommandValidator");
}
