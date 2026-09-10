// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Specifications;
using Xunit;

namespace Cratis.Stage.Contracts.for_EventModelLoader;

public class when_loading_a_single_file_or_folder : given.a_compiled_invoicing_model
{
    string _directory = null!;
    string _file = null!;
    EventModel _fromFile = null!;
    EventModel _fromFolder = null!;

    void Establish()
    {
        _directory = Path.Combine(Path.GetTempPath(), $"stage input parity {Guid.NewGuid():N}");
        Directory.CreateDirectory(_directory);
        _file = Path.Combine(_directory, "input.PLAY");
        File.WriteAllText(_file, Source);
        File.WriteAllText(Path.Combine(_directory, "ignored.play.txt"), "not Screenplay");
    }

    async Task Because()
    {
        _fromFile = await EventModelLoader.LoadFromPathAsync(_file);
        _fromFolder = await EventModelLoader.LoadFromPathAsync(_directory);
    }

    [Fact] void should_preserve_all_serialized_identities_and_structure() => EventModelFile.Write(_fromFile).ShouldEqual(EventModelFile.Write(_fromFolder));
    [Fact] void should_preserve_the_source_model_identity() => _fromFile.Id.ShouldEqual(_model.Id);
    [Fact] void should_discover_a_nonempty_application() => _fromFile.Collections.Single().Modules.Count.ShouldEqual(1);
    [Fact] void should_keep_all_four_slices() => _fromFile.Collections.Single().Modules.Single().Features.Single().Slices.Count.ShouldEqual(4);
    [Fact] void should_keep_the_modeled_specification() => _fromFile.Collections.Single().Modules.Single().Features.Single().Slices.Single(slice => slice.Name == "RegisterInvoice").Specifications.Count.ShouldEqual(1);
    [Fact] void should_not_require_the_symbolic_implementation_file() => File.Exists(Path.Combine(_directory, "Reactors", "Notifier.cs")).ShouldBeFalse();

    void Destroy()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }
}
#endif
