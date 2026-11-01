// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.Naming.Imports;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.CodeGeneration.for_CSharpImports;

public class when_classifying_unknown_scope : Specification
{
    const string Known = "namespace Shop.Orders;\n\npublic class PlaceOrder { public global::System.Guid Id { get; init; } }\n";

    static UnknownScopeReason Classify(string source, bool declared = false)
    {
        var imports = new CSharpImports(GeneratedProjects.Application, [], true);
        if (declared) imports.Unknown();
        return imports.Classify(source, []);
    }

    [Fact] void should_report_none_for_a_plain_file() => Classify(Known).ShouldEqual(UnknownScopeReason.None);
    [Fact] void should_report_a_scope_the_renderer_declared_unknown() => Classify(Known, declared: true).ShouldEqual(UnknownScopeReason.Declared);
    [Fact] void should_report_an_uncataloged_base_class() => Classify("namespace Shop.Orders;\n\npublic class PlaceOrder : global::Shop.External.Base { }\n").ShouldEqual(UnknownScopeReason.UnknownBase);
    [Fact] void should_report_a_block_namespace() => Classify("namespace Shop.Orders\n{\n    public class PlaceOrder { }\n}\n").ShouldEqual(UnknownScopeReason.NotFileScopedNamespace);
    [Fact] void should_report_several_namespaces() => Classify("namespace Shop.Orders;\n\npublic class A { }\n\nnamespace Shop.Other;\n\npublic class B { }\n").ShouldEqual(UnknownScopeReason.NotFileScopedNamespace);
    [Fact] void should_report_own_usings() => Classify("using System;\n\nnamespace Shop.Orders;\n\npublic class PlaceOrder { }\n").ShouldEqual(UnknownScopeReason.OwnUsings);
    [Fact] void should_report_a_syntax_error() => Classify("namespace Shop.Orders;\n\npublic class PlaceOrder {\n").ShouldEqual(UnknownScopeReason.ParseError);
    [Fact] void should_report_a_conditional_directive() => Classify("namespace Shop.Orders;\n\n#if DEBUG\npublic class PlaceOrder { }\n#endif\n").ShouldEqual(UnknownScopeReason.ConditionalDirective);
}
