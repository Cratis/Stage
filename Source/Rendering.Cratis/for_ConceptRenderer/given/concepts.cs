// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Specifications;

namespace Cratis.Stage.Rendering.Cratis.for_ConceptRenderer.given;

public class concepts : Specification
{
    protected ApplicationSet _applicationSet = null!;
    protected ConceptSyntax _invoiceId = null!;
    protected ConceptSyntax _money = null!;
    protected ConceptSyntax _discountPercentage = null!;
    protected ConceptSyntax _invoiceStatus = null!;
    protected ConceptSyntax _emailAddress = null!;

    void Establish()
    {
        _invoiceId = new ConceptSyntax("InvoiceId", "Uuid", [], [], SourceLocation.Start);
        _money = new ConceptSyntax("Money", "Decimal", [], [], SourceLocation.Start);

        var minRule = new ValidationRuleSyntax(
            ValidationRuleSyntax.ConceptValue,
            ValidationRuleKind.Min,
            new LiteralExpressionSyntax(0d, SourceLocation.Start),
            "A discount cannot be negative",
            SourceLocation.Start);

        var maxRule = new ValidationRuleSyntax(
            ValidationRuleSyntax.ConceptValue,
            ValidationRuleKind.Max,
            new LiteralExpressionSyntax(100d, SourceLocation.Start),
            "A discount cannot exceed 100 percent",
            SourceLocation.Start);

        var customRule = new ValidationRuleSyntax(
            ValidationRuleSyntax.ConceptValue,
            ValidationRuleKind.Rule,
            null,
            "Must be a round number",
            SourceLocation.Start,
            Code: new CodeBlockSyntax("csharp", "return Value % 1 == 0;", SourceLocation.Start));

        _discountPercentage = new ConceptSyntax(
            "DiscountPercentage",
            "Decimal",
            [],
            [],
            SourceLocation.Start,
            Validations: [new DeclarativeValidateSyntax([minRule, maxRule, customRule], SourceLocation.Start)]);

        _invoiceStatus = new ConceptSyntax("InvoiceStatus", "Enum", [], ["draft", "sent", "paid"], SourceLocation.Start);

        var piiAttribute = new ConceptAttributeSyntax(ConceptAttributeSyntax.Pii, SourceLocation.Start);
        _emailAddress = new ConceptSyntax("EmailAddress", "String", [piiAttribute], [], SourceLocation.Start);

        var identifierProperty = new PropertySyntax(
            "invoiceId",
            new TypeRefSyntax("InvoiceId", false, false, SourceLocation.Start),
            SourceLocation.Start,
            IsIdentifier: true);

        var command = new CommandSyntax("RegisterInvoice", [identifierProperty], null, [], [], null, SourceLocation.Start);

        var slice = new SliceSyntax(
            SliceType.StateChange,
            "Register",
            [],
            [command],
            [],
            [],
            [],
            [],
            [],
            [],
            [],
            SourceLocation.Start);

        var feature = new FeatureSyntax("Invoices", [], [slice], SourceLocation.Start);
        var module = new ModuleSyntax("Billing", [], [feature], SourceLocation.Start);

        var application = new ApplicationSyntax(
            [],
            [_invoiceId, _money, _discountPercentage, _invoiceStatus, _emailAddress],
            [],
            [module],
            SourceLocation.Start);

        _applicationSet = new ApplicationSet([application]);
    }
}
