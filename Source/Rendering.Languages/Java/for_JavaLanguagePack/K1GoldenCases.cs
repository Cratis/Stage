// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaLanguagePack;

public static class K1GoldenCases
{
    public static SymbolPlan Plan()
    {
        TypeName lower = new("vectors.collision", "order", []);
        TypeName upper = new("vectors.collision", "Order", []);
        TypeName outer = new("vectors.nesting", "Outer", []);
        TypeName inner = new("vectors.nesting", "Outer", ["Inner"]);
        TypeName foreign = new("library", "Outer", ["Inner"]);
        TypeName inherited = new("vectors.inheritance", "Child", []);
        TypeName baseType = new("library", "Base", []);
        TypeName text = new("java.lang", "String", []);
        TypeName unknown = new("vectors.unknown", "Child", []);
        TypeName obscured = new("vectors.obscured", "Child", []);
        TypeName prefixed = new("com.library", "Outer", ["Inner"]);
        var files = new SymbolFile[]
        {
            File("collision-lower", lower, [new ReferenceSegment(upper, "collision-upper", "collision-lower"), new TextSegment(" other;\n")]),
            File("collision-upper", upper, [new ReferenceSegment(lower, "collision-lower", "collision-upper"), new TextSegment(" other;\n")]),
            File("nesting", outer, [new TextSegment("static class "), new DeclarationSegment(inner, "inner", ParentOwner: "nesting") { Supertypes = KnownTypes<TypeName>.None }, new TextSegment(" {\n"), new ReferenceSegment(foreign, LexicalOwner: "inner"), new TextSegment(" value;\n}\n")]),
            File("inheritance", inherited, [new ReferenceSegment(text, LexicalOwner: "inheritance"), new TextSegment(" value;\n")], (KnownTypes<TypeName>.Of(baseType), [new TextSegment(" extends "), new ReferenceSegment(baseType, LexicalOwner: "inheritance")])),
            File("unknown", unknown, [new ReferenceSegment(text, LexicalOwner: "unknown"), new TextSegment(" value;\n"), new ReferenceSegment(text), new TextSegment(" omittedOwner;\n")], (KnownTypes<TypeName>.Unknown, [])),
            File("obscured", obscured, [new TextSegment("int Inner; int com;\n"), new ReferenceSegment(prefixed, LexicalOwner: "obscured"), new TextSegment(" value;\n")]),
        };

        return new(files, [foreign, text, prefixed])
        {
            ExternalDeclarations = [new(baseType, KnownTypes<string>.Of("String")) { Supertypes = KnownTypes<TypeName>.None }],
        };
    }

    static SymbolFile File(string id, TypeName type, SymbolSegment[] body, (KnownTypes<TypeName> Supertypes, SymbolSegment[] Header)? inheritance = null)
    {
        SymbolSegment[] segments = [new TextSegment("// Copyright (c) Cratis. All rights reserved.\n// Licensed under the MIT license. See LICENSE file in the project root for full license information.\n\npackage " + type.Package + ";\n"), new ImportsSegment(), new TextSegment("public class "), new DeclarationSegment(type, id) { Supertypes = inheritance?.Supertypes ?? KnownTypes<TypeName>.None }, .. inheritance?.Header ?? [], new TextSegment(" {\n"), .. body, new TextSegment("}\n")];

        return new(id, type.Package, segments);
    }
}
