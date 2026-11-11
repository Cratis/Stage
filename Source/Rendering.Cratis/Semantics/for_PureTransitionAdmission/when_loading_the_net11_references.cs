// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_PureTransitionAdmission;

public class when_loading_the_net11_references : Specification
{
    MetadataReference[] _references = null!;

    void Because() => _references = PureTransitionAdmission.AnalysisReferences("net11.0");

    [Fact] void should_load_a_non_empty_set() => _references.ShouldNotBeEmpty();
    [Fact] void should_contain_the_runtime_reference() => _references.Select(reference => reference.Display ?? string.Empty).Any(display => display.Contains("System.Runtime", StringComparison.Ordinal) || display.Contains("System.Private.CoreLib", StringComparison.Ordinal)).ShouldBeTrue();
}
