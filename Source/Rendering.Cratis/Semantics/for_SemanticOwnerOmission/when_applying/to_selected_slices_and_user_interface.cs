// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticOwnerOmission.when_applying;

public class to_selected_slices_and_user_interface : given.an_omission_context
{
    SemanticId _firstKept;
    SemanticId _secondKept;
    SemanticOwnerOmission _omission = null!;
    IReadOnlyList<LocatedSemanticSlice> _selected = null!;
    IReadOnlyList<LocatedSemanticSlice> _userInterface = null!;

    void Because()
    {
        BuildContext();
        _firstKept = Identity("first-kept-slice");
        _secondKept = Identity("second-kept-slice");
        _omission = SemanticOwnerOmission.Compute(_context, _index, Roots(_registerProject.Events.Single().Id))!;
        var all = _context.AllSlices;
        LocatedSemanticSlice Kept(SemanticId id) => all[0] with { Slice = all[0].Slice with { Id = id } };
        _selected = _omission.Apply([Kept(_firstKept), .. all.Take(1), Kept(_secondKept), .. all.Skip(1)]);
        _userInterface = _omission.Apply(all);
    }

    [Fact] void should_remove_exactly_the_closure_slices_from_the_selection() => _selected.Select(slice => slice.Slice.Id).SequenceEqual([_firstKept, _secondKept]).ShouldBeTrue();
    [Fact] void should_remove_every_closure_slice_from_the_user_interface() => _userInterface.ShouldBeEmpty();
    [Fact] void should_omit_both_slices() => _omission.Closure.ShouldContain(_registerProject.Id, _projectLookup.Id);
}
