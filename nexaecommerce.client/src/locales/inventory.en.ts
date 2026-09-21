const inventoryEn = {
    nav: {
        inventoryManagement:
            'Inventory',

        inventoryTransfers:
            'Transfers',
    },

    inventory: {
        reservationTitle:
            'Stock reservation',

        reservationDescription:
            'Reserve available stock for an order before payment is completed.',

        reserveStock:
            'Reserve stock',

        releaseReservation:
            'Release reservation',

        commitReservation:
            'Commit reservation',

        reservationCreated:
            'Stock reserved successfully.',

        reservationReleased:
            'Reservation released successfully.',

        reservationCommitted:
            'Reservation committed successfully.',

        reservationCreateError:
            'Stock could not be reserved.',

        reservationReleaseError:
            'Reservation could not be released.',

        reservationCommitError:
            'Reservation could not be committed.',

        reservationExpired:
            'Expired',

        reservationPending:
            'Pending',

        reservationCommittedStatus:
            'Committed',

        reservationReleasedStatus:
            'Released',

        reservationQuantity:
            'Reserved quantity',

        reservationExpires:
            'Expires',

        insufficientStock:
            'Not enough available stock.',
        title:
            'Inventory management',

        description:
            'Manage warehouse stock for catalog products and product variants.',

        warehouses:
            'Warehouses',

        goToWarehouses:
            'Manage warehouses',

        catalogProduct:
            'New catalog product',

        backToInventory:
            'Inventory',

        loading:
            'Loading…',

        noActiveWarehouses:
            'No active warehouse is available.',

        noActiveWarehousesDescription:
            'Create and activate a warehouse before assigning stock.',

        productLoadError:
            "We couldn't load the product.",

        noActiveLocations:
            'No active location is available.',

        noActiveLocationsDescription:
            'Create an active location before assigning stock.',

        goToWarehouse:
            'Manage warehouse locations',

        warehousesTitle:
            'Warehouses',

        productInventoryTitle:
            'Warehouse inventory',

        productInventoryDescription:
            'Manage stock for a catalog product variant at warehouse locations.',

        variant:
            'Product variant',

        warehouse:
            'Warehouse',

        location:
            'Location',

        onHand:
            'On hand',

        reorderPoint:
            'Reorder point',

        totalAvailable:
            'Total available',

        saveStock:
            'Save stock',

        stockSaved:
            'Stock saved successfully.',

        stockSaveError:
            "We couldn't save stock.",

        currentLocations:
            'Current stock locations',

        noStockLocations:
            'No warehouse stock has been recorded for this variant yet.',

        onHandShort:
            'On hand',

        reservedShort:
            'Reserved',

        availableShort:
            'Available',

        singleProductRule:
            'A product is created once in the catalog. Warehouse stock only records where its variants are physically stored.',

        noActiveVariants:
            'This product has no active variants.',

        selectVariantWarehouseLocation:
            'Select a variant, warehouse and active location.',

        activeInventory:
            'Active inventory',

        catalogOnly:
            'Catalog products only',

        searchPlaceholder:
            'Search by product name or SKU…',

        noProductsTitle:
            'No products found',

        noProductsDescription:
            'Create a product in the catalog first, then manage its warehouse stock.',

        searchNoProductsDescription:
            'Try another product name or SKU.',

        registerProduct:
            'Create product in catalog',

        noManagePermission:
            'You need inventory management permission to edit product stock.',

        locationsTitle:
            'Warehouse locations',

        locationsDescription:
            '{{count}} active location(s) are available in this warehouse.',

        addLocation:
            'Add location',

        editLocation:
            'Edit location',

        newLocation:
            'New location',

        locationCode:
            'Location code',

        locationName:
            'Location name',

        locationPath:
            'Zone / Rack / Shelf / Bin',

        zone:
            'Zone',

        rack:
            'Rack',

        shelf:
            'Shelf',

        bin:
            'Bin',

        status:
            'Status',

        actions:
            'Actions',

        active:
            'Active',

        inactive:
            'Inactive',

        createLocation:
            'Create location',

        locationRequired:
            'Location code and name are required.',

        locationCreated:
            'Location created successfully.',

        locationUpdated:
            'Location updated successfully.',

        locationSaveError:
            "We couldn't save the location.",

        locationsLoadError:
            "We couldn't load warehouse locations.",

        noLocations:
            'No locations have been created for this warehouse yet.',

        locationDeactivated:
            'Location deactivated.',

        locationActivated:
            'Location activated.',

        locationActionError:
            "We couldn't complete that location action.",

        movementLedgerTitle:
            'Stock movement ledger',

        movementLedgerDescription:
            'Recent physical stock movements for the selected variant and location.',

        movementLoadError:
            "We couldn't load stock movement history.",

        noMovements:
            'No stock movements have been recorded yet.',

        movementDate:
            'Date',

        movementType:
            'Movement',

        movementDelta:
            'Change',

        movementBalance:
            'Balance after',

        movementReason:
            'Reason',

        movementTypes: {
            OpeningBalance:
                'Opening balance',

            Purchase:
                'Purchase',

            Receive:
                'Receive',

            AdjustmentIncrease:
                'Adjustment increase',

            AdjustmentDecrease:
                'Adjustment decrease',

            Damage:
                'Damage',

            TransferOut:
                'Transfer out',

            TransferIn:
                'Transfer in',

            Reservation:
                'Reservation',

            ReservationRelease:
                'Reservation release',

            Sale:
                'Sale',

            Return:
                'Return',

            Correction:
                'Correction',

            Picking:
                'Picking',
        },

    

        transfersTitle:
            'Warehouse transfers',

        transfersDescription:
            'Transfer a catalog product variant between warehouse locations without creating another product.',

        createTransferTitle:
            'Create transfer',

        createTransferDescription:
            'Select a catalog product, its variant, source location and destination location.',

        transferReadOnly:
            'You have read-only access to warehouse transfers.',

        transferProduct:
            'Catalog product',

        transferVariant:
            'Product variant',

        transferSource:
            'Source',

        transferDestination:
            'Destination',

        transferAvailable:
            'Available to transfer',

        transferQuantity:
            'Quantity',

        transferStatus:
            'Status',

        transferDate:
            'Date',

        transferReasonPlaceholder:
            'Optional transfer reason',

        createTransfer:
            'Transfer stock',

        transferRequiredFields:
            'Select the product, variant, source and destination.',

        transferSameLocation:
            'Source and destination cannot be the same location.',

        transferInvalidQuantity:
            'Transfer quantity must be greater than zero.',

        transferInsufficientStock:
            'The source location does not have enough available stock.',

        transferCreated:
            'Stock transfer completed successfully.',

        transferCreateError:
            'The stock transfer could not be completed.',

        transferHistory:
            'Transfer history',

        transferHistoryDescription:
            'Recent warehouse transfer records.',

        transferHistoryLoadError:
            'Transfer history could not be loaded.',

        transferHistoryEmpty:
            'No warehouse transfers have been recorded yet.',

        transferPage:
            'Page',

        transferStatusCompleted:
            'Completed',

        transferStatusPending:
            'Pending',

        transferStatusCancelled:
            'Cancelled',

        refresh:
            'Refresh',

        previous:
            'Previous',

        next:
            'Next',
    },
};

export default inventoryEn;