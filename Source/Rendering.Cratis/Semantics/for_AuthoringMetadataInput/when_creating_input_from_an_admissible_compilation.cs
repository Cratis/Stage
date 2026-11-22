// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_AuthoringMetadataInput;

public class when_creating_input_from_an_admissible_compilation : given.an_admissible_compilation
{
    AuthoringMetadataInput.Catalog? _catalog;
    bool _accepted;

    void Because()
    {
        var input = AuthoringMetadataInput.Create(_compilation);
        input.ShouldNotBeNull();
        _accepted = AuthoringMetadataInput.TryRead(input, _compilation.Model, out _catalog);
    }

    [Fact] void should_admit_the_metadata_it_creates() => _accepted.ShouldBeTrue();
    [Fact] void should_keep_surviving_command_documentation() => _catalog.Entries.Values.ShouldContain(new AuthoringMetadataInput.Metadata("Registers <project> & name", "# Command <notes> & details\n\n- **Keep** the requested name."));
    [Fact] void should_keep_surviving_read_model_documentation() => _catalog.Entries.Values.ShouldContain(new AuthoringMetadataInput.Metadata("Shows <project> & name", "# View <notes> & details\n\n- `name` may contain <markup> & text."));
    [Fact] void should_not_include_excluded_view_metadata() => _catalog.Entries.Values.ShouldNotContain(new AuthoringMetadataInput.Metadata("Excluded view", null));
    [Fact] void should_not_include_excluded_query_metadata() => _catalog.Entries.Values.ShouldNotContain(new AuthoringMetadataInput.Metadata("Excluded query", null));
    [Fact] void should_only_include_the_five_surviving_descriptions() => _catalog.Entries.Count.ShouldEqual(5);
}
