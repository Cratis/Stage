// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Specifications;
using Cratis.Stage.Contracts;
using Xunit;

namespace Cratis.Stage.Host.for_StageHttpSurface;

public class when_admitting_repeated_source_slices : Specification
{
    EventModel _model = null!;
    Exception? _failure;

    void Establish() => _model = EventModelLoader.LoadFromSource("""
        module ApartmentComplexes
          feature Maintenance
            slice StateChange ServiceApartmentUnitTakenOutOf
              command ServiceApartmentUnitTakenOutOf
                produces ApartmentUnitTakenOutOfService
              event ApartmentUnitTakenOutOfService
            slice StateChange ServiceApartmentUnitTakenOutOf
              command ServiceApartmentUnitTakenOutOf
                produces ApartmentUnitTakenOutOfService
              event ApartmentUnitTakenOutOfService
        """);

    void Because() => _failure = Catch.Exception(() => StageHttpSurface.Create(_model));

    [Fact] void should_reject_the_shared_generated_type_before_mapping() => _failure.ShouldBeOfExactType<AmbiguousStageHttpSurface>();
    [Fact] void should_name_the_authored_slice() => _failure!.Message.ShouldContain("ServiceApartmentUnitTakenOutOf");
    [Fact] void should_name_the_conflicting_route() => ((AmbiguousStageHttpSurface)_failure!).Path.ShouldEqual("/api/apartment-complexes/maintenance/service-apartment-unit-taken-out-of/service-apartment-unit-taken-out-of");
}
