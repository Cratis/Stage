// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Testing;
using Cratis.Chronicle.Testing.ReadModels;
using Cratis.Stage.Specifications.Types;
using Xunit.Abstractions;

namespace Cratis.Stage.Specifications.for_SemanticSpecificationExecutor;

// Native engine scenarios are nested so Screenplay does not export them as application slice specifications.
public static class when_running_root_joins_with_explicit_keys
{
    public class with_a_source_id_matching_a_decoy(ITestOutputHelper output) : Specification
    {
        ReadModelScenario<Root> _scenario;

        void Establish()
        {
            _scenario = new ReadModelScenario<Root>(null, new Defaults(new SemanticClientArtifactsProvider([typeof(Opened), typeof(Labeled)])))
                .WithProjection(builder =>
                {
                    builder.NoAutoMap();
                    builder.From<Opened>(from => from.Set(model => model.JoinValue).To(evt => evt.ForeignKey));
                    builder.Join<Labeled>(join =>
                    {
                        join.On(model => model.JoinValue);
                        join.UsingKey(evt => evt.Key);
                        join.Set(model => model.Label).To(evt => evt.Text);
                    });
                });
            _scenario.CollectEventsFor(new EventSourceId("target-root"), [new Opened("target-key")]);
            _scenario.CollectEventsFor(new EventSourceId("decoy-root"), [new Opened("decoy-key")]);
        }

        void Because()
        {
            _scenario.CollectEventsFor(new EventSourceId("decoy-key"), [new Labeled("target-key", "updated")]);
            foreach (var instance in _scenario.Instances)
            {
                output.WriteLine($"{instance.Key}: JoinValue={instance.Value.JoinValue}, Label={instance.Value.Label}");
            }
        }

        void Destroy() => _scenario.Dispose();

        [Fact] void should_update_the_property_matched_target() => _scenario.Instances.Single(instance => instance.Key.ToString() == "target-root").Value.Label.ShouldEqual("updated");

        [Fact] void should_leave_the_source_matched_decoy_unchanged() => _scenario.Instances.Single(instance => instance.Key.ToString() == "decoy-root").Value.Label.ShouldEqual("unchanged");

        [Fact] void should_not_create_an_instance_with_the_property_key() => _scenario.Instances.Keys.Select(key => key.ToString()).ShouldContainOnly("target-root", "decoy-root");

        [EventType]
        public record Opened(string ForeignKey);

        [EventType]
        public record Labeled(string Key, string Text);

        public class Root
        {
            public string JoinValue { get; set; } = string.Empty;

            public string Label { get; set; } = "unchanged";
        }
    }
}
