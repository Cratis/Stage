// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Stage.Host.for_SandboxIdentity.when_selecting_per_operation.given;

internal static class multimodule_source
{
    internal const string Source = """
        concept ItemId : Uuid
        policy IsShopper
          require role "Shopper"
        policy IsCatalog
          require role "Catalog"
        policy IsSales
          require role "Sales"
        policy Special
          require role "Special"
        policy Missing
          require role "Missing"
        persona SalesRep
          policy IsSales
        persona Merchandiser
          policy IsCatalog
          policy IsSales
          policy IsShopper
          policy Special
        persona Shopper
          policy IsShopper
        module Storefront
          authorize IsShopper
          screen template ShopShell
            category Shell
            scopes application
            topbar
            content topbar
              component Cratis.Blueprint.Default.userMenu account
                property name = "Sandbox Shopper"
                property role = "Shopper"
          feature Browse
            slice StateView ShopLookup
              readmodel ShopItem
                itemId ItemId
                name String
              query ShopItems => ShopItem[]
        module Catalog
          authorize IsCatalog
          feature Browse
            slice StateView CatalogLookup
              readmodel CatalogItem
                itemId ItemId
                name String
              query CatalogItems => CatalogItem[]
            slice StateChange Rename
              command RenameItem
                itemId ItemId identifier
                name String
                produces event ItemRenamed
                  name String = name
        module Sales
          authorize IsSales
          feature Browse
            slice StateView SalesLookup
              readmodel SalesItem
                itemId ItemId
                name String
              query SalesItems => SalesItem[]
              query SpecialItems => SalesItem[]
                authorize Special
              query EitherItems => SalesItem[]
                authorize Missing or Special
              query UnavailableItems => SalesItem[]
                authorize Missing
          feature Restricted
            authorize Special
            feature Nested
              slice StateView RestrictedLookup
                readmodel RestrictedItem
                  itemId ItemId
                  name String
                query RestrictedItems => RestrictedItem[]
                  authorize IsShopper
        module Public
          feature Browse
            slice StateView PublicLookup
              readmodel PublicItem
                itemId ItemId
                name String
              query PublicItems => PublicItem[]
        """;
}
