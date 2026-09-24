// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Commands;
using Cratis.Stage.Contracts.Screenplay;
using Xunit;

namespace Cratis.Stage.Contracts.for_ProducedValueConverter.when_converting_context_values;

public class and_the_value_is_tenant_or_caused_by : Specification
{
    (ProducedValueKind Kind, string Expression) _tenant;
    (ProducedValueKind Kind, string Expression) _subject;
    (ProducedValueKind Kind, string Expression) _name;
    (ProducedValueKind Kind, string Expression) _userName;

    void Because()
    {
        _tenant = ProducedValueConverter.Convert(new ContextExpressionSyntax("tenant", SourceLocation.Start));
        _subject = ProducedValueConverter.Convert(new ContextExpressionSyntax("causedBy.subject", SourceLocation.Start));
        _name = ProducedValueConverter.Convert(new ContextExpressionSyntax("causedBy.name", SourceLocation.Start));
        _userName = ProducedValueConverter.Convert(new ContextExpressionSyntax("causedBy.userName", SourceLocation.Start));
    }

    [Fact] void should_resolve_the_tenant() => _tenant.ShouldEqual((ProducedValueKind.Tenant, string.Empty));
    [Fact] void should_resolve_the_subject_to_the_caller_id() => _subject.ShouldEqual((ProducedValueKind.Identity, "id"));
    [Fact] void should_resolve_the_name() => _name.ShouldEqual((ProducedValueKind.Identity, "name"));
    [Fact] void should_resolve_the_user_name() => _userName.ShouldEqual((ProducedValueKind.Identity, "userName"));
}
