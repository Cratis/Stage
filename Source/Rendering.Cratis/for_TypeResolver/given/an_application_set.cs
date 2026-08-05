// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;

namespace Cratis.Stage.Rendering.Cratis.for_TypeResolver.given;

public class an_application_set : Specification
{
    protected ApplicationSet _applicationSet = null!;

    void Establish()
    {
        var moneyConcept = new ConceptSyntax("Money", "Decimal", [], [], SourceLocation.Start);
        var statusConcept = new ConceptSyntax("Status", "Enum", [], ["Draft", "Sent"], SourceLocation.Start);
        var addressType = new TypeSyntax("Address", [], SourceLocation.Start);

        var application = new ApplicationSyntax([], [moneyConcept, statusConcept], [], [], SourceLocation.Start, Types: [addressType]);

        _applicationSet = new ApplicationSet([application]);
    }
}
