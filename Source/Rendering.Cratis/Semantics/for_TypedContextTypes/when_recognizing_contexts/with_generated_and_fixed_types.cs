// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_TypedContextTypes.when_recognizing_contexts;

public class with_generated_and_fixed_types : Specification
{
    INamedTypeSymbol _wrapper = null!;
    INamedTypeSymbol _shape = null!;
    INamedTypeSymbol _fixed = null!;
    INamedTypeSymbol _elsewhere = null!;
    INamedTypeSymbol _peer = null!;
    INamedTypeSymbol _foreign = null!;
    readonly IReadOnlySet<string> _allocated = new HashSet<string>(StringComparer.Ordinal) { "App.TypedContexts.PlaceOrderContext", "App.TypedContexts.PlaceOrderContextAmountShape" };

    void Because()
    {
        var compilation = CSharpCompilation.Create(
            "Probe",
            [CSharpSyntaxTree.ParseText("namespace App.TypedContexts { public record PlaceOrderContext(int A); public record PlaceOrderContextAmountShape(int A); public record TenantId(string Value); public record NotAllocated(int A); } namespace Other.TypedContexts { public record PlaceOrderContext(int A); } namespace App.Other { public record PlaceOrderContext(int A); }")],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)]);
        _wrapper = compilation.GetTypeByMetadataName("App.TypedContexts.PlaceOrderContext")!;
        _shape = compilation.GetTypeByMetadataName("App.TypedContexts.PlaceOrderContextAmountShape")!;
        _fixed = compilation.GetTypeByMetadataName("App.TypedContexts.TenantId")!;
        _elsewhere = compilation.GetTypeByMetadataName("App.Other.PlaceOrderContext")!;
        _peer = compilation.GetTypeByMetadataName("App.TypedContexts.NotAllocated")!;
        _foreign = compilation.GetTypeByMetadataName("Other.TypedContexts.PlaceOrderContext")!;
    }

    [Fact] void should_recognize_a_generated_wrapper() => TypedContextTypes.IsContext(_wrapper, _allocated).ShouldBeTrue();
    [Fact] void should_recognize_a_generated_shape() => TypedContextTypes.IsContext(_shape, _allocated).ShouldBeTrue();
    [Fact] void should_not_recognize_a_fixed_runtime_name() => TypedContextTypes.IsContext(_fixed, _allocated).ShouldBeFalse();
    [Fact] void should_not_recognize_a_type_outside_the_typed_context_namespace() => TypedContextTypes.IsContext(_elsewhere, _allocated).ShouldBeFalse();
    [Fact] void should_not_recognize_an_unallocated_peer_in_the_namespace() => TypedContextTypes.IsContext(_peer, _allocated).ShouldBeFalse();
    [Fact] void should_not_recognize_another_roots_typed_context_with_the_same_name() => TypedContextTypes.IsContext(_foreign, _allocated).ShouldBeFalse();
    [Fact] void should_not_recognize_anything_when_nothing_is_allocated() => TypedContextTypes.IsContext(_wrapper, new HashSet<string>()).ShouldBeFalse();
    [Fact] void should_not_recognize_null() => TypedContextTypes.IsContext(null, _allocated).ShouldBeFalse();
}
#endif
