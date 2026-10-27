// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaLanguagePack.given;

public class a_resolved_plan : Specification
{
    protected static readonly TypeName Widget = new("example", "Widget", []);
    protected static readonly TypeName List = new("java.util", "List", []);
    protected static readonly TypeName StringType = new("java.lang", "String", []);

    protected static DeclarationSegment Declaration => new(Widget, "widget") { Supertypes = KnownTypes<TypeName>.None };

    protected static SymbolPlan Plan(params SymbolSegment[] body)
    {
        SymbolSegment[] segments = [new TextSegment("package example;\n"), new ImportsSegment(), new TextSegment("public class "), Declaration, new TextSegment(" {\n"), .. body, new TextSegment("}\n")];

        return new([new("widget", "example", segments)], [List, StringType]);
    }
}
