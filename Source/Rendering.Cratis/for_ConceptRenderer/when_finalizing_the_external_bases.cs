// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.for_CratisRenderer.given;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.for_ConceptRenderer;

public class when_finalizing_the_external_bases : an_application_with_external_bases
{
    async Task Because() => await _renderer.Render([_application], _targetDirectory, _output, _error);

    [Fact] void should_emit_the_short_identifier_base() => Artifact("/InvoiceId.cs").ShouldContain("public record InvoiceId(Guid Value) : EventSourceId<Guid>(Value)");
    [Fact] void should_import_the_identifier_base_namespace() => Artifact("/InvoiceId.cs").ShouldContain("using Cratis.Chronicle.Events;");
    [Fact] void should_not_leave_the_identifier_base_qualified() => Artifact("/InvoiceId.cs").ShouldNotContain("global::");
    [Fact] void should_emit_the_short_value_base() => Artifact("/InvoiceName.cs").ShouldContain("public record InvoiceName(string Value) : ConceptAs<string>(Value)");
    [Fact] void should_import_the_value_base_namespace() => Artifact("/InvoiceName.cs").ShouldContain("using Cratis.Concepts;");
    [Fact] void should_not_leave_the_value_base_qualified() => Artifact("/InvoiceName.cs").ShouldNotContain("global::");
    [Fact] void should_emit_the_short_validator_base() => Artifact("/InvoiceName.cs").ShouldContain("public class InvoiceNameValidator : ConceptValidator<InvoiceName>");
    [Fact] void should_import_the_validator_base_namespace() => Artifact("/InvoiceName.cs").ShouldContain("using Cratis.Arc.Validation;");
    [Fact] void should_not_leave_the_validator_base_qualified() => Artifact("/InvoiceName.cs").ShouldNotContain("global::");
}
