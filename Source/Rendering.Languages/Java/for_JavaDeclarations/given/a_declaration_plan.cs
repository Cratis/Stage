// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaDeclarations.given;

public class a_declaration_plan : Specification
{
    protected SymbolPlan _plan = null!;
    protected JavaDeclarations _declarations = null!;

    protected static SymbolFile File(string id, string name, string owner, string package = "example") =>
        new(id, package, [new DeclarationSegment(new(package, name, []), owner)]);
}
