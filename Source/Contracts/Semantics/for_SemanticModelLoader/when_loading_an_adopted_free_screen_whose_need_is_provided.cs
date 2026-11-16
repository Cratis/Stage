// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.Semantics.for_SemanticModelLoader;

public class when_loading_an_adopted_free_screen_whose_need_is_provided : given.a_folder_with_an_adopted_free_screen
{
    protected override string Provision => "        data Book via query AllBooks";

    async Task Because() => _result = await SemanticModelLoader.LoadAsync(_root, [], null, "Lending");

    [Fact] void should_load_the_model() => _result.Success.ShouldBeTrue();
    [Fact] void should_have_no_free_screen_refusal() => _result.Errors.Select(error => error.Code).ShouldNotContain(SemanticModelLoader.FreeScreenRefusalCode);
}
