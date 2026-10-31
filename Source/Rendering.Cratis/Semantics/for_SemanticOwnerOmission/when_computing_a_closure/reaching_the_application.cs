// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_SemanticOwnerOmission.when_computing_a_closure;

public class reaching_the_application : given.an_omission_context
{
    bool _application_closure;
    bool _module_closure;
    bool _feature_closure;
    bool _slice_closure;

    void Because()
    {
        BuildContext();
        _application_closure = SemanticOwnerOmission.IsProvable(_context, [_application.Id]);
        _module_closure = SemanticOwnerOmission.IsProvable(_context, [_module.Id]);
        _feature_closure = SemanticOwnerOmission.IsProvable(_context, [_feature.Id]);
        _slice_closure = SemanticOwnerOmission.IsProvable(_context, [_registerProject.Id]);
    }

    [Fact] void should_not_prove_a_closure_holding_the_application() => _application_closure.ShouldBeFalse();
    [Fact] void should_not_prove_a_closure_holding_a_module() => _module_closure.ShouldBeFalse();
    [Fact] void should_not_prove_a_closure_holding_a_feature() => _feature_closure.ShouldBeFalse();
    [Fact] void should_prove_a_closure_of_slices() => _slice_closure.ShouldBeTrue();
}
