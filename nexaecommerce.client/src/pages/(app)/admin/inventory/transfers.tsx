import {
    useEffect,
    useMemo,
    useState,
} from 'react';

import {
    Link,
} from 'react-router';

import {
    ArrowLeftRight,
    ChevronLeft,
    ChevronRight,
    MapPin,
    Package,
    RefreshCw,
    Warehouse as WarehouseIcon,
} from 'lucide-react';

import {
    useTranslation,
} from 'react-i18next';

import {
    toast,
} from 'sonner';

import {
    Alert,
    AlertDescription,
} from '@/components/ui/alert';

import {
    Badge,
} from '@/components/ui/badge';

import {
    Button,
} from '@/components/ui/button';

import {
    Card,
    CardContent,
    CardDescription,
    CardHeader,
    CardTitle,
} from '@/components/ui/card';

import {
    Input,
} from '@/components/ui/input';

import {
    Label,
} from '@/components/ui/label';

import {
    PageHeader,
} from '@/components/data-states';

import {
    usePermission,
} from '@/hooks/use-permission';

import {
    useAdminProducts,
    useProduct,
} from '@/modules/catalog/products/hooks';

import {
    useWarehouseStock,
} from '@/modules/inventory/hooks/useInventoryStock';

import {
    useWarehouseLocations,
    useWarehouses,
} from '@/modules/inventory/hooks/useWarehouses';

import {
    useCreateWarehouseTransfer,
    useWarehouseTransfers,
} from '@/modules/inventory/hooks/useWarehouseTransfers';

import {
    INVENTORY_PERM,
} from '@/lib/api/inventory';

const PAGE_SIZE = 20;

export default function WarehouseTransfersPage() {
    const {
        t,
        i18n,
    } = useTranslation();

    const isRtl =
        i18n.language
            .toLowerCase()
            .startsWith('fa') ||
        i18n.language
            .toLowerCase()
            .startsWith('ar');

    const canManage =
        usePermission(
            INVENTORY_PERM.manage,
        );

    const [
        page,
        setPage,
    ] = useState(1);

    const [
        productId,
        setProductId,
    ] = useState('');

    const [
        variantId,
        setVariantId,
    ] = useState('');

    const [
        sourceWarehouseId,
        setSourceWarehouseId,
    ] = useState('');

    const [
        sourceLocationId,
        setSourceLocationId,
    ] = useState('');

    const [
        destinationWarehouseId,
        setDestinationWarehouseId,
    ] = useState('');

    const [
        destinationLocationId,
        setDestinationLocationId,
    ] = useState('');

    const [
        quantity,
        setQuantity,
    ] = useState(1);

    const [
        reason,
        setReason,
    ] = useState('');

    const productsQuery =
        useAdminProducts({
            page: 1,
            pageSize: 100,
            sortBy: 'name',
            desc: false,
            isActive: true,
        });

    const warehousesQuery =
        useWarehouses(false);

    const products =
        productsQuery.data?.items ??
        [];

    const warehouses =
        (
            warehousesQuery.data ??
            []
        ).filter(
            warehouse =>
                warehouse.isActive,
        );

    const productQuery =
        useProduct(
            productId ||
            undefined,
        );

    const variants =
        useMemo(
            () =>
                (
                    productQuery
                        .data
                        ?.variants ??
                    []
                ).filter(
                    variant =>
                        variant.isActive,
                ),
            [
                productQuery
                    .data
                    ?.variants,
            ],
        );

    const sourceLocationsQuery =
        useWarehouseLocations(
            sourceWarehouseId ||
            undefined,
            false,
        );

    const destinationLocationsQuery =
        useWarehouseLocations(
            destinationWarehouseId ||
            undefined,
            false,
        );

    const sourceLocations =
        sourceLocationsQuery.data ??
        [];

    const destinationLocations =
        destinationLocationsQuery.data ??
        [];

    const sourceStockQuery =
        useWarehouseStock(
            sourceWarehouseId ||
            undefined,
            variantId ||
            undefined,
        );

    const sourceStocks =
        sourceStockQuery.data ??
        [];

    const selectedSourceStock =
        sourceStocks.find(
            stock =>
                stock.productVariantId ===
                variantId &&
                stock.locationId ===
                sourceLocationId,
        );

    const availableQuantity =
        selectedSourceStock
            ?.availableQuantity ??
        0;

    const createTransfer =
        useCreateWarehouseTransfer();

    const skip =
        (page - 1) *
        PAGE_SIZE;

    const transfersQuery =
        useWarehouseTransfers(
            skip,
            PAGE_SIZE,
        );

    const transfers =
        transfersQuery.data?.items ??
        [];

    useEffect(() => {
        if (
            !productId &&
            products.length > 0
        ) {
            setProductId(
                products[0].id,
            );
        }
    }, [
        productId,
        products,
    ]);

    useEffect(() => {
        if (
            productId &&
            !products.some(
                product =>
                    product.id ===
                    productId,
            )
        ) {
            setProductId(
                products[0]?.id ??
                '',
            );

            setVariantId('');
        }
    }, [
        productId,
        products,
    ]);

    useEffect(() => {
        if (
            !variantId &&
            variants.length > 0
        ) {
            setVariantId(
                variants[0].id,
            );
        }
    }, [
        variantId,
        variants,
    ]);

    useEffect(() => {
        if (
            variantId &&
            !variants.some(
                variant =>
                    variant.id ===
                    variantId,
            )
        ) {
            setVariantId(
                variants[0]?.id ??
                '',
            );
        }
    }, [
        variantId,
        variants,
    ]);

    useEffect(() => {
        if (
            !sourceWarehouseId &&
            warehouses.length > 0
        ) {
            setSourceWarehouseId(
                warehouses[0].id,
            );
        }
    }, [
        sourceWarehouseId,
        warehouses,
    ]);

    useEffect(() => {
        if (
            !destinationWarehouseId &&
            warehouses.length > 0
        ) {
            const secondWarehouse =
                warehouses.find(
                    warehouse =>
                        warehouse.id !==
                        sourceWarehouseId,
                );

            setDestinationWarehouseId(
                secondWarehouse?.id ??
                warehouses[0].id,
            );
        }
    }, [
        destinationWarehouseId,
        sourceWarehouseId,
        warehouses,
    ]);

    useEffect(() => {
        if (
            sourceWarehouseId &&
            !warehouses.some(
                warehouse =>
                    warehouse.id ===
                    sourceWarehouseId,
            )
        ) {
            setSourceWarehouseId(
                warehouses[0]?.id ??
                '',
            );

            setSourceLocationId('');
        }
    }, [
        sourceWarehouseId,
        warehouses,
    ]);

    useEffect(() => {
        if (
            destinationWarehouseId &&
            !warehouses.some(
                warehouse =>
                    warehouse.id ===
                    destinationWarehouseId,
            )
        ) {
            setDestinationWarehouseId(
                warehouses[0]?.id ??
                '',
            );

            setDestinationLocationId(
                '',
            );
        }
    }, [
        destinationWarehouseId,
        warehouses,
    ]);

    useEffect(() => {
        if (
            !sourceLocationId &&
            sourceLocations.length >
            0
        ) {
            setSourceLocationId(
                sourceLocations[0].id,
            );
        }
    }, [
        sourceLocationId,
        sourceLocations,
    ]);

    useEffect(() => {
        if (
            !destinationLocationId &&
            destinationLocations.length >
            0
        ) {
            setDestinationLocationId(
                destinationLocations[0].id,
            );
        }
    }, [
        destinationLocationId,
        destinationLocations,
    ]);

    useEffect(() => {
        if (
            sourceLocationId &&
            !sourceLocations.some(
                location =>
                    location.id ===
                    sourceLocationId,
            )
        ) {
            setSourceLocationId(
                sourceLocations[0]?.id ??
                '',
            );
        }
    }, [
        sourceLocationId,
        sourceLocations,
    ]);

    useEffect(() => {
        if (
            destinationLocationId &&
            !destinationLocations.some(
                location =>
                    location.id ===
                    destinationLocationId,
            )
        ) {
            setDestinationLocationId(
                destinationLocations[0]?.id ??
                '',
            );
        }
    }, [
        destinationLocationId,
        destinationLocations,
    ]);

    const sameLocation =
        sourceWarehouseId ===
        destinationWarehouseId &&
        sourceLocationId ===
        destinationLocationId;

    const submitTransfer =
        async () => {
            if (!canManage) {
                return;
            }

            if (
                !variantId ||
                !sourceWarehouseId ||
                !sourceLocationId ||
                !destinationWarehouseId ||
                !destinationLocationId
            ) {
                toast.error(
                    t(
                        'inventory.transferRequiredFields',
                    ),
                );

                return;
            }

            if (sameLocation) {
                toast.error(
                    t(
                        'inventory.transferSameLocation',
                    ),
                );

                return;
            }

            const normalizedQuantity =
                Math.trunc(
                    Number(
                        quantity,
                    ),
                );

            if (
                normalizedQuantity <=
                0
            ) {
                toast.error(
                    t(
                        'inventory.transferInvalidQuantity',
                    ),
                );

                return;
            }

            if (
                normalizedQuantity >
                availableQuantity
            ) {
                toast.error(
                    t(
                        'inventory.transferInsufficientStock',
                    ),
                );

                return;
            }

            try {
                await createTransfer.mutateAsync(
                    {
                        sourceWarehouseId,
                        sourceLocationId,
                        destinationWarehouseId,
                        destinationLocationId,
                        productVariantId:
                            variantId,
                        quantity:
                            normalizedQuantity,
                        reason:
                            reason.trim() ||
                            null,
                    },
                );

                toast.success(
                    t(
                        'inventory.transferCreated',
                    ),
                );

                setQuantity(1);
                setReason('');
            } catch (error) {
                toast.error(
                    error instanceof Error
                        ? error.message
                        : t(
                            'inventory.transferCreateError',
                        ),
                );
            }
        };

    const warehouseName =
        (
            id: string,
        ) =>
            warehouses.find(
                warehouse =>
                    warehouse.id ===
                    id,
            )?.name ??
            id.slice(0, 8);

    const locationName =
        (
            warehouseId: string,
            locationId: string,
        ) => {
            const sourceMatch =
                sourceWarehouseId ===
                    warehouseId
                    ? sourceLocations.find(
                        location =>
                            location.id ===
                            locationId,
                    )
                    : undefined;

            if (
                sourceMatch
            ) {
                return sourceMatch.name;
            }

            const destinationMatch =
                destinationWarehouseId ===
                    warehouseId
                    ? destinationLocations.find(
                        location =>
                            location.id ===
                            locationId,
                    )
                    : undefined;

            if (
                destinationMatch
            ) {
                return destinationMatch.name;
            }

            return locationId.slice(
                0,
                8,
            );
        };

    const statusLabel =
        (
            status: string,
        ) => {
            switch (
            status.toLowerCase()
            ) {
                case 'completed':
                    return t(
                        'inventory.transferStatusCompleted',
                    );

                case 'pending':
                    return t(
                        'inventory.transferStatusPending',
                    );

                case 'cancelled':
                    return t(
                        'inventory.transferStatusCancelled',
                    );

                default:
                    return status;
            }
        };

    return (
        <div
            className="space-y-6"
            dir={
                isRtl
                    ? 'rtl'
                    : 'ltr'
            }
        >
            <PageHeader
                title={t(
                    'inventory.transfersTitle',
                )}
                description={t(
                    'inventory.transfersDescription',
                )}
                actions={
                    <div className="flex flex-wrap gap-2">
                        <Button
                            variant="outline"
                            asChild
                        >
                            <Link to="/admin/inventory">
                                <Package />
                                {t(
                                    'inventory.backToInventory',
                                )}
                            </Link>
                        </Button>

                        <Button
                            variant="outline"
                            asChild
                        >
                            <Link to="/admin/warehouses">
                                <WarehouseIcon />
                                {t(
                                    'inventory.warehouses',
                                )}
                            </Link>
                        </Button>
                    </div>
                }
            />

            {warehouses.length ===
                0 && (
                    <Card>
                        <CardContent className="pt-6">
                            <Alert>
                                <WarehouseIcon />

                                <AlertDescription>
                                    <div className="space-y-3">
                                        <div className="font-medium">
                                            {t(
                                                'inventory.noActiveWarehouses',
                                            )}
                                        </div>

                                        <Button
                                            asChild
                                            variant="outline"
                                        >
                                            <Link to="/admin/warehouses/new">
                                                {t(
                                                    'inventory.goToWarehouses',
                                                )}
                                            </Link>
                                        </Button>
                                    </div>
                                </AlertDescription>
                            </Alert>
                        </CardContent>
                    </Card>
                )}

            {warehouses.length >
                0 &&
                canManage && (
                    <Card>
                        <CardHeader>
                            <div className="flex items-start gap-3">
                                <div className="rounded-lg border p-2">
                                    <ArrowLeftRight className="size-5" />
                                </div>

                                <div>
                                    <CardTitle>
                                        {t(
                                            'inventory.createTransferTitle',
                                        )}
                                    </CardTitle>

                                    <CardDescription>
                                        {t(
                                            'inventory.createTransferDescription',
                                        )}
                                    </CardDescription>
                                </div>
                            </div>
                        </CardHeader>

                        <CardContent className="space-y-6">
                            <div className="grid gap-4 md:grid-cols-2">
                                <div className="grid gap-2">
                                    <Label htmlFor="transfer-product">
                                        {t(
                                            'inventory.transferProduct',
                                        )}
                                    </Label>

                                    <select
                                        id="transfer-product"
                                        className="border-input bg-background h-10 w-full rounded-md border px-3 text-sm"
                                        value={
                                            productId
                                        }
                                        onChange={
                                            event => {
                                                setProductId(
                                                    event
                                                        .target
                                                        .value,
                                                );

                                                setVariantId(
                                                    '',
                                                );
                                            }
                                        }
                                    >
                                        {products.map(
                                            product => (
                                                <option
                                                    key={
                                                        product.id
                                                    }
                                                    value={
                                                        product.id
                                                    }
                                                >
                                                    {
                                                        product.name
                                                    }
                                                    {' — '}
                                                    {
                                                        product.sku
                                                    }
                                                </option>
                                            ),
                                        )}
                                    </select>
                                </div>

                                <div className="grid gap-2">
                                    <Label htmlFor="transfer-variant">
                                        {t(
                                            'inventory.transferVariant',
                                        )}
                                    </Label>

                                    <select
                                        id="transfer-variant"
                                        className="border-input bg-background h-10 w-full rounded-md border px-3 text-sm"
                                        value={
                                            variantId
                                        }
                                        onChange={
                                            event =>
                                                setVariantId(
                                                    event
                                                        .target
                                                        .value,
                                                )
                                        }
                                        disabled={
                                            variants.length ===
                                            0
                                        }
                                    >
                                        {variants.map(
                                            variant => (
                                                <option
                                                    key={
                                                        variant.id
                                                    }
                                                    value={
                                                        variant.id
                                                    }
                                                >
                                                    {
                                                        variant.sku
                                                    }

                                                    {variant.color
                                                        ? ` — ${variant.color}`
                                                        : ''}

                                                    {variant.size
                                                        ? ` — ${variant.size}`
                                                        : ''}
                                                </option>
                                            ),
                                        )}
                                    </select>
                                </div>
                            </div>

                            <div className="grid gap-4 md:grid-cols-2">
                                <Card>
                                    <CardHeader>
                                        <CardTitle className="text-base">
                                            {t(
                                                'inventory.transferSource',
                                            )}
                                        </CardTitle>
                                    </CardHeader>

                                    <CardContent className="space-y-4">
                                        <div className="grid gap-2">
                                            <Label htmlFor="source-warehouse">
                                                {t(
                                                    'inventory.warehouse',
                                                )}
                                            </Label>

                                            <select
                                                id="source-warehouse"
                                                className="border-input bg-background h-10 w-full rounded-md border px-3 text-sm"
                                                value={
                                                    sourceWarehouseId
                                                }
                                                onChange={
                                                    event => {
                                                        setSourceWarehouseId(
                                                            event
                                                                .target
                                                                .value,
                                                        );

                                                        setSourceLocationId(
                                                            '',
                                                        );
                                                    }
                                                }
                                            >
                                                {warehouses.map(
                                                    warehouse => (
                                                        <option
                                                            key={
                                                                warehouse.id
                                                            }
                                                            value={
                                                                warehouse.id
                                                            }
                                                        >
                                                            {
                                                                warehouse.code
                                                            }
                                                            {' — '}
                                                            {
                                                                warehouse.name
                                                            }
                                                        </option>
                                                    ),
                                                )}
                                            </select>
                                        </div>

                                        <div className="grid gap-2">
                                            <Label htmlFor="source-location">
                                                {t(
                                                    'inventory.location',
                                                )}
                                            </Label>

                                            <select
                                                id="source-location"
                                                className="border-input bg-background h-10 w-full rounded-md border px-3 text-sm"
                                                value={
                                                    sourceLocationId
                                                }
                                                onChange={
                                                    event =>
                                                        setSourceLocationId(
                                                            event
                                                                .target
                                                                .value,
                                                        )
                                                }
                                                disabled={
                                                    sourceLocations.length ===
                                                    0
                                                }
                                            >
                                                {sourceLocations.map(
                                                    location => (
                                                        <option
                                                            key={
                                                                location.id
                                                            }
                                                            value={
                                                                location.id
                                                            }
                                                        >
                                                            {
                                                                location.code
                                                            }
                                                            {' — '}
                                                            {
                                                                location.name
                                                            }
                                                        </option>
                                                    ),
                                                )}
                                            </select>
                                        </div>

                                        <div className="rounded-lg border p-4">
                                            <div className="text-muted-foreground text-xs">
                                                {t(
                                                    'inventory.transferAvailable',
                                                )}
                                            </div>

                                            <div className="mt-1 text-2xl font-bold">
                                                {
                                                    availableQuantity
                                                }
                                            </div>
                                        </div>
                                    </CardContent>
                                </Card>

                                <Card>
                                    <CardHeader>
                                        <CardTitle className="text-base">
                                            {t(
                                                'inventory.transferDestination',
                                            )}
                                        </CardTitle>
                                    </CardHeader>

                                    <CardContent className="space-y-4">
                                        <div className="grid gap-2">
                                            <Label htmlFor="destination-warehouse">
                                                {t(
                                                    'inventory.warehouse',
                                                )}
                                            </Label>

                                            <select
                                                id="destination-warehouse"
                                                className="border-input bg-background h-10 w-full rounded-md border px-3 text-sm"
                                                value={
                                                    destinationWarehouseId
                                                }
                                                onChange={
                                                    event => {
                                                        setDestinationWarehouseId(
                                                            event
                                                                .target
                                                                .value,
                                                        );

                                                        setDestinationLocationId(
                                                            '',
                                                        );
                                                    }
                                                }
                                            >
                                                {warehouses.map(
                                                    warehouse => (
                                                        <option
                                                            key={
                                                                warehouse.id
                                                            }
                                                            value={
                                                                warehouse.id
                                                            }
                                                        >
                                                            {
                                                                warehouse.code
                                                            }
                                                            {' — '}
                                                            {
                                                                warehouse.name
                                                            }
                                                        </option>
                                                    ),
                                                )}
                                            </select>
                                        </div>

                                        <div className="grid gap-2">
                                            <Label htmlFor="destination-location">
                                                {t(
                                                    'inventory.location',
                                                )}
                                            </Label>

                                            <select
                                                id="destination-location"
                                                className="border-input bg-background h-10 w-full rounded-md border px-3 text-sm"
                                                value={
                                                    destinationLocationId
                                                }
                                                onChange={
                                                    event =>
                                                        setDestinationLocationId(
                                                            event
                                                                .target
                                                                .value,
                                                        )
                                                }
                                                disabled={
                                                    destinationLocations.length ===
                                                    0
                                                }
                                            >
                                                {destinationLocations.map(
                                                    location => (
                                                        <option
                                                            key={
                                                                location.id
                                                            }
                                                            value={
                                                                location.id
                                                            }
                                                        >
                                                            {
                                                                location.code
                                                            }
                                                            {' — '}
                                                            {
                                                                location.name
                                                            }
                                                        </option>
                                                    ),
                                                )}
                                            </select>
                                        </div>
                                    </CardContent>
                                </Card>
                            </div>

                            {sameLocation && (
                                <Alert variant="destructive">
                                    <AlertDescription>
                                        {t(
                                            'inventory.transferSameLocation',
                                        )}
                                    </AlertDescription>
                                </Alert>
                            )}

                            <div className="grid gap-4 md:grid-cols-2">
                                <div className="grid gap-2">
                                    <Label htmlFor="transfer-quantity">
                                        {t(
                                            'inventory.transferQuantity',
                                        )}
                                    </Label>

                                    <Input
                                        id="transfer-quantity"
                                        type="number"
                                        min={1}
                                        max={
                                            availableQuantity
                                        }
                                        value={
                                            quantity
                                        }
                                        onChange={
                                            event =>
                                                setQuantity(
                                                    Number(
                                                        event
                                                            .target
                                                            .value,
                                                    ) ||
                                                    0,
                                                )
                                        }
                                    />
                                </div>

                                <div className="grid gap-2">
                                    <Label htmlFor="transfer-reason">
                                        {t(
                                            'inventory.movementReason',
                                        )}
                                    </Label>

                                    <Input
                                        id="transfer-reason"
                                        value={
                                            reason
                                        }
                                        onChange={
                                            event =>
                                                setReason(
                                                    event
                                                        .target
                                                        .value,
                                                )
                                        }
                                        placeholder={t(
                                            'inventory.transferReasonPlaceholder',
                                        )}
                                    />
                                </div>
                            </div>

                            <div className="rounded-lg border bg-muted/20 p-4">
                                <div className="grid gap-4 md:grid-cols-3">
                                    <Summary
                                        label={t(
                                            'inventory.transferProduct',
                                        )}
                                        value={
                                            productQuery
                                                .data
                                                ?.name ??
                                            '—'
                                        }
                                    />

                                    <Summary
                                        label={t(
                                            'inventory.transferVariant',
                                        )}
                                        value={
                                            variants.find(
                                                variant =>
                                                    variant.id ===
                                                    variantId,
                                            )
                                                ?.sku ??
                                            '—'
                                        }
                                    />

                                    <Summary
                                        label={t(
                                            'inventory.transferQuantity',
                                        )}
                                        value={String(
                                            quantity,
                                        )}
                                    />
                                </div>
                            </div>

                            <div className="flex justify-end">
                                <Button
                                    onClick={() =>
                                        void submitTransfer()
                                    }
                                    disabled={
                                        createTransfer.isPending ||
                                        availableQuantity <=
                                        0 ||
                                        sameLocation ||
                                        !sourceLocationId ||
                                        !destinationLocationId
                                    }
                                >
                                    <ArrowLeftRight />

                                    {t(
                                        'inventory.createTransfer',
                                    )}
                                </Button>
                            </div>
                        </CardContent>
                    </Card>
                )}

            {!canManage &&
                warehouses.length >
                0 && (
                    <Alert>
                        <AlertDescription>
                            {t(
                                'inventory.transferReadOnly',
                            )}
                        </AlertDescription>
                    </Alert>
                )}

            <Card>
                <CardHeader>
                    <div className="flex flex-wrap items-center justify-between gap-3">
                        <div>
                            <CardTitle>
                                {t(
                                    'inventory.transferHistory',
                                )}
                            </CardTitle>

                            <CardDescription>
                                {t(
                                    'inventory.transferHistoryDescription',
                                )}
                            </CardDescription>
                        </div>

                        <Button
                            variant="outline"
                            size="sm"
                            onClick={() =>
                                void transfersQuery.refetch()
                            }
                            disabled={
                                transfersQuery.isFetching
                            }
                        >
                            <RefreshCw
                                className={
                                    transfersQuery.isFetching
                                        ? 'animate-spin'
                                        : ''
                                }
                            />

                            {t(
                                'inventory.refresh',
                            )}
                        </Button>
                    </div>
                </CardHeader>

                <CardContent>
                    {transfersQuery.isLoading && (
                        <TransferTableSkeleton />
                    )}

                    {transfersQuery.isError && (
                        <Alert variant="destructive">
                            <AlertDescription>
                                {t(
                                    'inventory.transferHistoryLoadError',
                                )}
                            </AlertDescription>
                        </Alert>
                    )}

                    {transfersQuery.isSuccess &&
                        transfers.length ===
                        0 && (
                            <div className="rounded-lg border border-dashed p-10 text-center">
                                <ArrowLeftRight className="mx-auto size-10 text-muted-foreground" />

                                <div className="mt-4 font-medium">
                                    {t(
                                        'inventory.transferHistoryEmpty',
                                    )}
                                </div>
                            </div>
                        )}

                    {transfersQuery.isSuccess &&
                        transfers.length >
                        0 && (
                            <>
                                <div className="overflow-x-auto rounded-lg border">
                                    <table className="min-w-[1100px] w-full text-sm">
                                        <thead>
                                            <tr className="border-b bg-muted/30">
                                                <th className="px-4 py-3 text-start">
                                                    {t(
                                                        'inventory.transferProduct',
                                                    )}
                                                </th>

                                                <th className="px-4 py-3 text-start">
                                                    {t(
                                                        'inventory.transferSource',
                                                    )}
                                                </th>

                                                <th className="px-4 py-3 text-start">
                                                    {t(
                                                        'inventory.transferDestination',
                                                    )}
                                                </th>

                                                <th className="px-4 py-3 text-start">
                                                    {t(
                                                        'inventory.transferQuantity',
                                                    )}
                                                </th>

                                                <th className="px-4 py-3 text-start">
                                                    {t(
                                                        'inventory.transferStatus',
                                                    )}
                                                </th>

                                                <th className="px-4 py-3 text-start">
                                                    {t(
                                                        'inventory.transferDate',
                                                    )}
                                                </th>

                                                <th className="px-4 py-3 text-start">
                                                    {t(
                                                        'inventory.movementReason',
                                                    )}
                                                </th>
                                            </tr>
                                        </thead>

                                        <tbody>
                                            {transfers.map(
                                                transfer => (
                                                    <tr
                                                        key={
                                                            transfer.id
                                                        }
                                                        className="border-b last:border-0"
                                                    >
                                                        <td className="px-4 py-4">
                                                            <div className="font-medium">
                                                                {transfer.productVariantId.slice(
                                                                    0,
                                                                    8,
                                                                )}
                                                            </div>
                                                        </td>

                                                        <td className="px-4 py-4">
                                                            <div className="flex items-center gap-2">
                                                                <WarehouseIcon className="size-4" />

                                                                {
                                                                    warehouseName(
                                                                        transfer.sourceWarehouseId,
                                                                    )
                                                                }
                                                            </div>

                                                            <div className="mt-1 flex items-center gap-2 text-xs text-muted-foreground">
                                                                <MapPin className="size-3.5" />

                                                                {
                                                                    locationName(
                                                                        transfer.sourceWarehouseId,
                                                                        transfer.sourceLocationId,
                                                                    )
                                                                }
                                                            </div>
                                                        </td>

                                                        <td className="px-4 py-4">
                                                            <div className="flex items-center gap-2">
                                                                <WarehouseIcon className="size-4" />

                                                                {
                                                                    warehouseName(
                                                                        transfer.destinationWarehouseId,
                                                                    )
                                                                }
                                                            </div>

                                                            <div className="mt-1 flex items-center gap-2 text-xs text-muted-foreground">
                                                                <MapPin className="size-3.5" />

                                                                {
                                                                    locationName(
                                                                        transfer.destinationWarehouseId,
                                                                        transfer.destinationLocationId,
                                                                    )
                                                                }
                                                            </div>
                                                        </td>

                                                        <td className="px-4 py-4 font-semibold">
                                                            {transfer.quantity.toLocaleString(
                                                                isRtl
                                                                    ? 'fa-IR'
                                                                    : undefined,
                                                            )}
                                                        </td>

                                                        <td className="px-4 py-4">
                                                            <Badge variant="outline">
                                                                {
                                                                    statusLabel(
                                                                        transfer.status,
                                                                    )
                                                                }
                                                            </Badge>
                                                        </td>

                                                        <td className="px-4 py-4 whitespace-nowrap">
                                                            {formatDate(
                                                                transfer.completedAt ??
                                                                transfer.requestedAt,
                                                                isRtl,
                                                            )}
                                                        </td>

                                                        <td className="px-4 py-4 text-muted-foreground">
                                                            {
                                                                transfer.reason
                                                                ??
                                                                '—'}
                                                        </td>
                                                    </tr>
                                                ),
                                            )}
                                        </tbody>
                                    </table>
                                </div>

                                <div className="mt-5 flex items-center justify-between gap-3">
                                    <div className="text-sm text-muted-foreground">
                                        {t(
                                            'inventory.transferPage',
                                        )}{' '}
                                        {page}
                                    </div>

                                    <div className="flex gap-2">
                                        <Button
                                            variant="outline"
                                            size="sm"
                                            disabled={
                                                page <=
                                                1
                                            }
                                            onClick={() =>
                                                setPage(
                                                    current =>
                                                        Math.max(
                                                            1,
                                                            current -
                                                            1,
                                                        ),
                                                )
                                            }
                                        >
                                            <ChevronLeft />

                                            {t(
                                                'inventory.previous',
                                            )}
                                        </Button>

                                        <Button
                                            variant="outline"
                                            size="sm"
                                            disabled={
                                                transfers.length <
                                                PAGE_SIZE
                                            }
                                            onClick={() =>
                                                setPage(
                                                    current =>
                                                        current +
                                                        1,
                                                )
                                            }
                                        >
                                            {t(
                                                'inventory.next',
                                            )}

                                            <ChevronRight />
                                        </Button>
                                    </div>
                                </div>
                            </>
                        )}
                </CardContent>
            </Card>
        </div>
    );
}

function Summary({
    label,
    value,
}: {
    label: string;
    value: string;
}) {
    return (
        <div>
            <div className="text-muted-foreground text-xs">
                {label}
            </div>

            <div className="mt-1 font-medium">
                {value}
            </div>
        </div>
    );
}

function TransferTableSkeleton() {
    return (
        <div className="space-y-3">
            {[
                1,
                2,
                3,
                4,
            ].map(
                item => (
                    <div
                        key={item}
                        className="animate-pulse rounded-lg border p-5"
                    >
                        <div className="h-4 w-48 rounded bg-muted" />

                        <div className="mt-3 h-4 w-72 rounded bg-muted" />

                        <div className="mt-3 h-4 w-32 rounded bg-muted" />
                    </div>
                ),
            )}
        </div>
    );
}

function formatDate(
    value: string,
    rtl: boolean,
) {
    try {
        return new Intl.DateTimeFormat(
            rtl
                ? 'fa-IR'
                : undefined,
            {
                dateStyle:
                    'short',
                timeStyle:
                    'short',
            },
        ).format(
            new Date(value),
        );
    } catch {
        return value;
    }
}