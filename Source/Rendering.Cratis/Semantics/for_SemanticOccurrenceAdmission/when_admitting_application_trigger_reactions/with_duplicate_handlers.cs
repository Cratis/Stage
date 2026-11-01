// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticOccurrenceAdmission.when_admitting_application_trigger_reactions;

public class with_duplicate_handlers : given.an_application_trigger_reaction
{
    SemanticSlice _slice = null!;
    SemanticVersionFeature[] _features = [];
    SemanticVersionFeature[] _admitted = [];

    void Establish()
    {
        _slice = _located.Slice with { Reactions = [_reaction, _reaction with { Id = SemanticId.Parse($"sem1:{new string('a', 64)}"), Name = "ProcessAgain" }] };
        var module = _model.Application.Modules.Single();
        var feature = module.Features.Single();
        SetContext(ExecutableSemanticModel.Create(_model.LanguageVersion, _model.SemanticVersion, _model.Application with
        {
            Modules = [module with { Features = [feature with { Slices = [_slice] }] }]
        }));
    }

    void Because()
    {
        _features = [.. SemanticOccurrenceAdmission.RefusalsInSlice(_slice, _context)];
        _admitted = [.. SemanticOccurrenceAdmission.AdmittedTriggers(_context)];
    }

    [Fact] void should_refuse_both_handlers() => _features.Length.ShouldEqual(2);
    [Fact] void should_name_the_duplicate_binding() => _features.ShouldEachConformTo(item => item.Message.Contains("more than one reaction", StringComparison.Ordinal));
    [Fact] void should_not_admit_the_shared_trigger() => _admitted.ShouldBeEmpty();
}
