// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Specifications;
using Cratis.Stage.Contracts.Scene;
using Cratis.Stage.Host.for_SemanticRuntime.given;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Cratis.Stage.Host.for_StageRoutes;

public partial class when_describing_whether_a_command_creates_a_listed_row : Specification
{
    StageRoutes _routes = null!;

    async Task Because()
    {
        var plan = compiled_plan.From(Source + "\n" + NegativeSource);
        await using var app = WebApplication.CreateBuilder().Build();
        var scene = new SceneApplication([], [], [], [], [], []);
        _routes = StageRoutes.For(new StageSceneRoutes(scene, app.Services, NullLogger.Instance), plan.Model);
    }

    bool Creates(string command)
    {
        using var schema = JsonDocument.Parse(_routes.CommandSchemas[command]);
        return schema.RootElement.TryGetProperty("x-creates", out var mark) && mark.GetBoolean();
    }

    [Fact] void should_mark_register_product() => Creates("RegisterProduct").ShouldBeTrue();
    [Fact] void should_mark_register_invoice() => Creates("RegisterInvoice").ShouldBeTrue();
    [Fact] void should_mark_register_booking() => Creates("RegisterBooking").ShouldBeTrue();
    [Fact] void should_mark_register_role() => Creates("RegisterRole").ShouldBeTrue();
    [Fact] void should_mark_register_resource() => Creates("RegisterResource").ShouldBeTrue();
    [Fact] void should_mark_the_single_key_establishing_event_among_update_handlers() => Creates("InviteUser").ShouldBeTrue();
    [Fact] void should_not_mark_change_user_role() => Creates("ChangeUserRole").ShouldBeFalse();
    [Fact] void should_not_mark_an_ambiguous_creation() => Creates("RegisterSummary").ShouldBeFalse();
    [Fact] void should_report_the_ambiguous_creation_as_information() => _routes.Diagnostics.Any(message => message.Contains("STAGE-INTENT-001", StringComparison.Ordinal) && message.Contains("RegisterSummary", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_keep_marking_a_generated_identifier() => Creates("PlanShipment").ShouldBeTrue();
    [Fact] void should_not_mark_a_child_create_command() => Creates("AddInvoiceLine").ShouldBeFalse();
    [Fact] void should_report_the_parent_selection_as_information() => _routes.Diagnostics.Any(message => message.Contains("STAGE-INTENT-001", StringComparison.Ordinal) && message.Contains("AddInvoiceLine", StringComparison.Ordinal)).ShouldBeTrue();

    const string Source = """
        concept ProductId : Uuid
        concept InvoiceId : Uuid
        concept BookingId : Uuid
        concept RoleId : Uuid
        concept ResourceId : Uuid
        concept UserId : Uuid
        concept SummaryId : Uuid
        concept LineId : Uuid
        concept ShipmentId : Uuid
        module Business
          feature Registration
            slice StateChange RegisterProduct
              command RegisterProduct
                productId ProductId identifier
                name String
                produces ProductRegistered
                  for productId
                  name = name
              event ProductRegistered
                name String
            slice StateView Products
              readmodel Products
                productId ProductId
                name String
              query AllProducts => Products[]
              projection ProductsProjection => Products
                from ProductRegistered
                  productId = $eventSourceId
                  name = name
            slice StateChange RegisterInvoice
              command RegisterInvoice
                invoiceId InvoiceId identifier
                number String
                produces InvoiceRegistered
                  for invoiceId
                  number = number
              event InvoiceRegistered
                number String
            slice StateView Invoices
              readmodel Invoices
                invoiceId InvoiceId
                number String
              query AllInvoices => Invoices[]
              projection InvoicesProjection => Invoices
                from InvoiceRegistered
                  invoiceId = $eventSourceId
                  number = number
            slice StateChange RegisterBooking
              command RegisterBooking
                bookingId BookingId identifier
                reference String
                produces BookingRegistered
                  for bookingId
                  reference = reference
              event BookingRegistered
                reference String
            slice StateView Bookings
              readmodel Bookings
                bookingId BookingId
                reference String
              query AllBookings => Bookings[]
              projection BookingsProjection => Bookings
                from BookingRegistered
                  bookingId = $eventSourceId
                  reference = reference
            slice StateChange RegisterRole
              command RegisterRole
                roleId RoleId identifier
                name String
                produces RoleRegistered
                  for roleId
                  name = name
              event RoleRegistered
                name String
            slice StateView Roles
              readmodel Roles
                roleId RoleId
                name String
              query AllRoles => Roles[]
              projection RolesProjection => Roles
                from RoleRegistered
                  roleId = $eventSourceId
                  name = name
            slice StateChange RegisterResource
              command RegisterResource
                resourceId ResourceId identifier
                name String
                produces ResourceRegistered
                  for resourceId
                  name = name
              event ResourceRegistered
                name String
            slice StateView Resources
              readmodel Resources
                resourceId ResourceId
                name String
              query AllResources => Resources[]
              projection ResourcesProjection => Resources
                from ResourceRegistered
                  resourceId = $eventSourceId
                  name = name
        """;
}
