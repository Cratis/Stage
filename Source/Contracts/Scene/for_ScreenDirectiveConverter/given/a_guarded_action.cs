// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;

namespace Cratis.Stage.Contracts.Scene.for_ScreenDirectiveConverter.given;

public class a_guarded_action : Specification
{
    protected ScreenGuardedActionSyntax _action = null!;
    protected SourceLocation _location = new(12, 5, "Invoices.play");

    void Establish() => _action = new(
        "Process invoice",
        [
            new ScreenActionAlternativeSyntax(
                new ComparisonConditionSyntax("status", ComparisonOperator.Equal, new LiteralExpressionSyntax("draft", _location), _location),
                "SendInvoice",
                _location)
        ],
        _location);
}
