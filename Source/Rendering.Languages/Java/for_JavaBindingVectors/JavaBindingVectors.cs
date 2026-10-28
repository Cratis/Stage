// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Stage.Rendering.Declarative;
using Cratis.Stage.Rendering.Languages.Java.for_JavaLanguagePack;
using static Cratis.Stage.Rendering.Languages.Java.for_JavaLanguagePack.given.a_java_pack;
using static Cratis.Stage.Rendering.Languages.Java.for_JavaPlan.given.a_java_plan;

namespace Cratis.Stage.Rendering.Languages.Java.for_JavaBindingVectors;

public static class JavaBindingVectors
{
    public static ImmutableArray<JavaBindingCase> Positive() =>
    [
        new("k1-golden-cases", K1GoldenCases.Files(), K1GoldenCases.FactsJson, [], ["vectors/unknown/Child.java"]),
        Inherited("plan-supertype-inherits-external", FactsJson),
        Case("anonymous-subclass", Class("example", "A", Txt("Object o = new "), Reference("Base", "lib"), Txt("() {\n    "), Reference("String", "java.lang"), Txt(" s;\n};\n"))),
        new("host-type-named-like-java-lang", [Class("vectors.hostshadow", "A", Reference("String", "java.lang"), Txt(" s;\n"))], """{"types":{}}""", [("vectors/hostshadow/String.java", "package vectors.hostshadow;\n\npublic class String {}\n")], []),
        Case("plan-type-named-like-java-lang", Class("example", "A", Reference("String"), Txt(" a;\n"), Reference("String", "java.lang"), Txt(" b;\n")), Class("example", "String")),
        Case("foreign-pair", Class("example", "A", Reference("Thing", "lib"), Txt(" first;\n"), Reference("Thing", "other"), Txt(" second;\n"))),
        Case("foreign-and-same-package", Class("example", "A", Reference("Plain", "lib"), Txt(" p;\n"), Reference("B"), Txt(" b;\n")), Class("example", "B")),
        Case("short-name-equals-package-head", Class("example", "A", Reference("Item", "q"), Txt(" i;\n"), Reference("Y", "Item.z"), Txt(" y;\n"))),
        Case("nested-self-reference", Nested("Outer", Reference("Outer"), Txt(" parent;\n"))),
        Case("jdk-types", Class("example", "A", Reference("List", "java.util"), Txt("<"), Reference("UUID", "java.util"), Txt("> ids;\n"), Reference("BigDecimal", "java.math"), Txt(" d;\n"), Reference("LocalDate", "java.time"), Txt(" day;\n"), Reference("OffsetDateTime", "java.time"), Txt(" at;\n"), Reference("Long", "java.lang"), Txt(" n;\n"), Reference("Boolean", "java.lang"), Txt(" b;\n")))
    ];

    public static JavaBindingCase Negative() => Inherited("under-included-facts", """{"types":{"lib.Base":[]}}""");

    static JavaBindingCase Case(string name, params DeclarativeLanguageFile[] files) => new(name, [.. files], FactsJson, [], []);

    static JavaBindingCase Inherited(string name, string facts) => new(
        name,
        [
            File("Mid.java", Imports(), Txt("public class "), Declaration("Mid"), Txt(" extends "), Reference("Base", "lib"), Txt(" {}\n")),
            File("A.java", Imports(), Txt("public class "), Declaration("A"), Txt(" extends "), Reference("Mid"), Txt(" {\n  "), Reference("String", "java.lang"), Txt(" s;\n}\n"))
        ],
        facts,
        [],
        []);
}
