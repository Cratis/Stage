// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Host.for_StageRoutes;

public partial class when_describing_whether_a_command_creates_a_listed_row
{
    const string NegativeSource = """
            slice StateChange ChangeUserRole
              command InviteUser
                userId UserId identifier
                role String
                produces UserInvited
                  for userId
                  role = role
              command ChangeUserRole
                userId UserId identifier
                role String
                produces UserRoleChanged
                  for userId
                  role = role
              event UserRoleChanged
                role String
              event UserInvited
                role String
            slice StateView Users
              readmodel Users
                userId UserId
                role String
              query AllUsers => Users[]
              projection UsersProjection => Users
                from UserInvited
                  userId = $eventSourceId
                  role = role
                from UserRoleChanged
                  role = role
            slice StateChange RegisterSummary
              command RegisterSummary
                summaryId SummaryId identifier
                produces SummaryRegistered
                  for summaryId
                  summaryId = summaryId
              event SummaryRegistered
                summaryId SummaryId
              event SummaryIssued
                summaryId SummaryId
              event SummaryCancelled
                summaryId SummaryId
            slice StateView Summaries
              readmodel Summaries
                summaryId SummaryId
                status String
              query AllSummaries => Summaries[]
              projection SummariesProjection => Summaries
                from SummaryRegistered key summaryId
                  status = "Draft"
                from SummaryIssued key summaryId
                  status = "Issued"
                from SummaryCancelled key summaryId
                  status = "Cancelled"
            slice StateChange AddInvoiceLine
              command AddInvoiceLine
                lineId LineId identifier
                invoiceId InvoiceId
                produces InvoiceLineAdded
                  for lineId
                  invoiceId = invoiceId
              event InvoiceLineAdded
                invoiceId InvoiceId
            slice StateView Lines
              readmodel Lines
                lineId LineId
                invoiceId InvoiceId
              query AllLines => Lines[]
              query GetLine => Lines optional
                by lineId LineId
              projection LinesProjection => Lines
                from InvoiceLineAdded
                  lineId = $eventSourceId
                  invoiceId = invoiceId
            slice StateChange PlanShipment
              command PlanShipment
                shipmentId ShipmentId generated identifier
                produces ShipmentPlanned
              event ShipmentPlanned
        """;
}
