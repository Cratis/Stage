// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Reflection;
using System.Text.Json;
using Cratis.Arc.Availability;
using Cratis.Arc.Slices;
using Cratis.Choreography.Availability;
using Cratis.Choreography.Availability.Audiences;
using Cratis.Specifications;
using Cratis.Stage.Rendering.Cratis.Semantics.for_ProvidedValues.given;
using Cratis.Types;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NSubstitute;
using Xunit;

namespace Cratis.Stage.Rendering.Cratis.Semantics.for_ProvidedValues;

public class when_publishing_screen_marker_parts : Specification
{
    Type[] _markers = [];
    IReadOnlyCollection<Type> _registered = [];
    IReadOnlyList<UnavailablePart> _unavailable = [];
    string _json = string.Empty;

    async Task Because()
    {
        var plan = a_fleet_model.Plan(an_available_screen_model.Source);
        var source = a_fleet_model.Text(plan, "ScreenParts.cs");
        var compilation = CSharpCompilation.Create("ScreenParts", [CSharpSyntaxTree.ParseText(source)], PureTransitionAdmission.AnalysisReferences(), new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        await using var output = new MemoryStream();
        var emitted = compilation.Emit(output);
        if (!emitted.Success) throw new InvalidOperationException(string.Join("; ", emitted.Diagnostics));
        _markers = Assembly.Load(output.ToArray()).GetTypes();
        RegisterMarkers();

        var gate = new a_screen_availability_gate(_registered, _markers[0]);
        _unavailable = await new UnavailableParts(gate, Substitute.For<IServiceProvider>()).Get();
        _json = JsonSerializer.Serialize(new { isSuccess = true, data = new AvailableParts(_unavailable) }, JsonSerializerOptions.Web);
    }

    void RegisterMarkers()
    {
        var options = new ArcAvailabilityOptions();
        foreach (var marker in _markers) options.Gate(marker, AvailabilityExpression.For([new AudienceName("Plus")]));
        var types = Substitute.For<ITypes>();
        types.All.Returns([]);
        _registered = new ArcAvailabilityGate(options, Substitute.For<IAvailabilityEvaluator>(), Substitute.For<IAudienceSource>(), types).GatedParts;
    }

    [Fact] void should_compile_every_marker() => _markers.Length.ShouldEqual(2);
    [Fact] void should_assign_no_arc_slice_to_any_marker() => _markers.All(type => SliceLocation.For(type).Equals(SliceLocation.NotSet)).ShouldBeTrue();
    [Fact] void should_discover_registered_markers_that_are_not_commands_or_read_models() => _registered.Count.ShouldEqual(2);
    [Fact] void should_close_only_the_denied_marker() => _unavailable.Count.ShouldEqual(1);
    [Fact] void should_publish_the_exact_closed_type_name() => _unavailable[0].Name.ShouldEqual(_markers[0].FullName);
    [Fact] void should_serialize_the_pinned_arc_client_contract() => _json.ShouldContain($"\"unavailable\":[{{\"name\":\"{_markers[0].FullName}\",\"module\":\"\",\"feature\":\"\",\"slice\":\"\"}}");
    [Fact] void should_not_list_the_available_marker() => _json.Contains(_markers[1].FullName!, StringComparison.Ordinal).ShouldBeFalse();
}
#endif
