// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Cratis.Stage.Rendering.Cratis.for_CratisArtifactRenderPlanner.given;

public static class opaque_policy_specification_model
{
    public const string Body = "return context.Identity.HasRole(\"Clerk\");";

    public static string ClockSource() => opaque_policy_model.Source("return context.Occurred.Year == 2027;").Replace(
        "      event InvoiceIssued\n",
        """
              specification AllowingTheAuthoredYear
                given clock "2027-03-04T12:00:00Z"
                given caller
                  authenticated
                when IssueInvoice
                  invoiceId = "invoice-one"
                  description = "North"
                then InvoiceIssued
                  invoiceId = "invoice-one"
                  description = "North"
              specification DenyingTheOtherYear
                given clock "2028-03-04T12:00:00Z"
                given caller
                  authenticated
                when IssueInvoice
                  invoiceId = "invoice-one"
                  description = "North"
                then denied
              event InvoiceIssued

        """,
        StringComparison.Ordinal);

    public static string SubjectSource() => opaque_policy_model.Source("return context.Subject == \"invoice-one\";").Replace(
        "      event InvoiceIssued\n",
        """
              specification AllowingTheSubject
                given caller
                  authenticated
                when IssueInvoice
                  invoiceId = "invoice-one"
                  description = "North"
                then InvoiceIssued
                  invoiceId = "invoice-one"
                  description = "North"
              specification DenyingTheOtherSubject
                given caller
                  authenticated
                when IssueInvoice
                  invoiceId = "invoice-two"
                  description = "North"
                then denied
              event InvoiceIssued

        """,
        StringComparison.Ordinal) + "\n" + """
              specification LookingUpTheSubject
                given caller
                  authenticated
                then query InvoiceById
                  arguments
                    invoiceId = "invoice-one"
              specification LookingUpAnotherSubject
                given caller
                  authenticated
                then query InvoiceById
                  arguments
                    invoiceId = "invoice-two"
                then denied
        """;

    public static string Source(string body = Body) => opaque_policy_model.Source(body).Replace(
        "      event InvoiceIssued\n",
        """
              specification AllowingTheClerk
                given caller
                  authenticated
                  role "Clerk"
                when IssueInvoice
                  invoiceId = "invoice-one"
                  description = "North"
                then InvoiceIssued
                  invoiceId = "invoice-one"
                  description = "North"
              specification DenyingTheVisitor
                given caller
                  authenticated
                when IssueInvoice
                  invoiceId = "invoice-one"
                  description = "North"
                then denied
              event InvoiceIssued

        """,
        StringComparison.Ordinal) + "\n" + """
              specification LookingUpAsClerk
                given caller
                  authenticated
                  role "Clerk"
                then query InvoiceById
                  arguments
                    invoiceId = "invoice-one"
              specification LookingUpAsVisitor
                given caller
                  authenticated
                then query InvoiceById
                  arguments
                    invoiceId = "invoice-one"
                then denied
        """;
}
#endif
