// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Contracts.Scene.for_OutletConverter.given;

/// <summary>
/// A catalog whose list template declares outlets of every kind, a dialog template that declares its actions, and a feature that refines,
/// hides, places and contributes to them - the outlet examples of the Screenplay 6 documentation in one model.
/// </summary>
public class a_translated_catalog : Scene.given.a_translated_application
{
    protected const string Catalog =
        """
        module Catalog
          screen template ListPage
            category List
            fits slot content
            header
            main
            detail pane routed by sku optional
            outlet toolbar of actions in header
              default refresh title "Refresh" runs refresh
              default export title "Export"
            outlet rowActions of actions for item
            outlet filters of filters in header
            outlet stats of stats in header
            outlet detailTabs of tabs in detail

          dialog template TaskDialog
            category Dialog
            body
            footer
            outlet actions of actions in footer
              default cancel title "Cancel" runs close
              default save title "Save" emphasis primary runs submit

          feature Products
            outlet toolbar extends
              hide export
              button add title "Add product" slice DraftProduct emphasis primary icon plus
                before refresh
              group more title "More" icon dots
                button retire title "Retire" slice WithdrawProduct
            outlet rowActions extends
              button price title "Set price" slice SetProductPrice opens in slot detail
              button withdraw title "Withdraw" slice WithdrawProduct
                runs
                enabled when item.status == "onSale"
                confirm "Withdraw this product?"
            outlet filters extends
              filter status title "Status" sets AllProducts.status
            outlet stats extends
              stat drafts title "Drafts" value from ProductStats.drafts
            contribute to detailTabs
              tab history title "History" shows ProductHistory

            slice StateChange DraftProduct
              command DraftProduct
                sku String identifier
                produces ProductDrafted
                  for sku
              event ProductDrafted
                sku String
              screen DraftProduct
                action DraftProduct

            slice StateChange SetProductPrice
              command SetProductPrice
                sku String identifier
                price Decimal
                produces PriceSet
                  for sku
              event PriceSet
                sku String
              screen SetProductPrice
                fits pane, drawer
                action SetProductPrice for screen

            slice StateChange WithdrawProduct
              command WithdrawProduct
                sku String identifier
                produces ProductWithdrawn
                  for sku
              event ProductWithdrawn
                sku String
              screen WithdrawProduct
                action WithdrawProduct

            slice StateView ProductCatalog
              readmodel ProductRow
                sku String
                status String
              readmodel ProductStats
                drafts Int
              query AllProducts => ProductRow[]
                filter status String
              query ProductStats => ProductStats
              screen ProductCatalog
                template ListPage
                outlet toolbar inherits
              screen ProductHistory
                title "History"
        """;

    void Establish() => Translate(Catalog);
}
