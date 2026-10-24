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
              command ChangeUserRoleWithParent
                userId UserId identifier
                invoiceId InvoiceId
                produces UserRoleChanged
                  for userId
                  role = "Support"
              command RecordAudit
                userId UserId identifier
                produces AuditRecorded
                  for userId
              event AuditRecorded
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
            slice StateChange AddCustomerNotes
              command RegisterCustomer
                customerKey CustomerKey identifier
                produces CustomerRegistered
                  for customerKey
              event CustomerRegistered
              command AddCustomerNote
                noteId NoteId identifier
                accountKey CustomerKey
                produces CustomerNoteAdded
                  for noteId
                  accountKey = accountKey
              command AddNamedCustomerNote
                noteId NoteId identifier
                customerKey String
                produces NamedCustomerNoteAdded
                  for noteId
                  customerKey = customerKey
              event CustomerNoteAdded
                accountKey CustomerKey
              event NamedCustomerNoteAdded
                customerKey String
            slice StateView CustomerNotes
              readmodel CustomerNotes
                noteId NoteId
                accountKey CustomerKey
              query AllCustomerNotes => CustomerNotes[]
              query GetCustomerNote => CustomerNotes optional
                by noteId NoteId
              projection CustomerNotesProjection => CustomerNotes
                from CustomerNoteAdded
                  noteId = $eventSourceId
                  accountKey = accountKey
            slice StateView NamedCustomerNotes
              readmodel NamedCustomerNotes
                noteId NoteId
                customerKey String
              query AllNamedCustomerNotes => NamedCustomerNotes[]
              query GetNamedCustomerNote => NamedCustomerNotes optional
                by noteId NoteId
              projection NamedCustomerNotesProjection => NamedCustomerNotes
                from NamedCustomerNoteAdded
                  noteId = $eventSourceId
                  customerKey = customerKey
            slice StateChange AddRawLines
              command AddRawLine
                lineId LineId identifier
                parentToken Uuid
                produces RawLineAdded
                  for lineId
                  parentToken = parentToken
              event RawLineAdded
                parentToken Uuid
            slice StateView RawLines
              readmodel RawLines
                lineId LineId
                parentToken Uuid
              query AllRawLines => RawLines[]
              query GetRawLine => RawLines optional
                by lineId LineId
              projection RawLinesProjection => RawLines
                from RawLineAdded
                  lineId = $eventSourceId
                  parentToken = parentToken
            slice StateChange PlanShipment
              command PlanShipment
                shipmentId ShipmentId generated identifier
                produces ShipmentPlanned
              event ShipmentPlanned
        """;
}
