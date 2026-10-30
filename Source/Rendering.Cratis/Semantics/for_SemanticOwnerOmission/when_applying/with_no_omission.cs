// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticOwnerOmission.when_applying;

public class with_no_omission : given.an_omission_context
{
    IReadOnlyList<LocatedSemanticSlice> _slices = null!;
    IReadOnlyList<LocatedSemanticSlice> _result = null!;
    IEnumerable<SemanticTypedContextDescriptor> _descriptors = null!;
    IEnumerable<SemanticTypedContextDescriptor> _descriptorResult = null!;
    SemanticOwnerOmission? _computed;

    void Because()
    {
        BuildContext();
        _slices = _context.SelectedSlices();
        _result = SemanticOwnerOmission.None.Apply(_slices);
        _descriptors = _context.Request.TypedContextDescriptors;
        _descriptorResult = SemanticOwnerOmission.None.Apply(_descriptors);
        _computed = SemanticOwnerOmission.Compute(_context, _index, Roots(_registerProject.Events.Single().Id).Clear());
    }

    [Fact] void should_be_empty() => SemanticOwnerOmission.None.IsEmpty.ShouldBeTrue();
    [Fact] void should_return_the_same_slices() => ReferenceEquals(_result, _slices).ShouldBeTrue();
    [Fact] void should_return_the_same_descriptors() => ReferenceEquals(_descriptorResult, _descriptors).ShouldBeTrue();
    [Fact] void should_compute_the_empty_omission_for_no_roots() => ReferenceEquals(_computed, SemanticOwnerOmission.None).ShouldBeTrue();
}
