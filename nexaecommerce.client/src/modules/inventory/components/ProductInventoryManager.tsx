/* eslint-disable react-hooks/set-state-in-effect */

import {
    useEffect,
    useMemo,
    useState,
} from 'react';

import { Link } from 'react-router';

import { useTranslation } from 'react-i18next';

import {
    Boxes,
    MapPin,
    Package,
    Warehouse as WarehouseIcon,
} from 'lucide-react';

import { toast } from 'sonner';

import {
    Alert,
    AlertDescription,
} from '@/components/ui/alert';

import { Badge } from '@/components/ui/badge';

import { Button } from '@/components/ui/button';

import {
    Card,
    CardContent,
    CardDescription,
    CardHeader,
    CardTitle,
} from '@/components/ui/card';

import { Input } from '@/components/ui/input';

import { Label } from '@/components/ui/label';

import {
    useProduct,
} from '@/modules/catalog/products/hooks';

import {
    useProductWarehouseStocks,
    useSetWarehouseStock,
    useWarehouseStockMovements,
} from '@/modules/inventory/hooks/useInventoryStock';

import {
    useWarehouseLocations,
} from '@/modules/inventory/hooks/useWarehouses';

import type {
    ProductVariant,
} from '@/modules/catalog/api/products';

export default function ProductInventoryManager({
    productId,
}: {
    productId: string;
}) {
    const { t } =
        useTranslation();

    const productQuery =
        useProduct(productId);

    const [
        variantId,
        setVariantId,
    ] = useState('');

    const [
        warehouseId,
        setWarehouseId,
    ] = useState('');

    const [
        locationId,
        setLocationId,
    ] = useState('');

    const [
        onHand,
        setOnHand,
    ] = useState(0);

    const [
        reorderPoint,
        setReorderPoint,
    ] = useState(0);

    const variants =
        useMemo(
            () =>
                (
                    productQuery
                        .data
                        ?.variants ??
                    []
                ).filter(
                    (variant) =>
                        variant.isActive,
                ),

            [
                productQuery
                    .data
                    ?.variants,
            ],
        );
    useEffect(() => {
        setVariantId('');
        setWarehouseId('');
        setLocationId('');
        setOnHand(0);
        setReorderPoint(0);
    }, [productId]);
    useEffect(() => {
        if (
            !variantId &&
            variants[0]?.id
        ) {
            setVariantId(
                variants[0].id,
            );
        }
    }, [
        variantId,
        variants,
    ]);

    const stockState =
        useProductWarehouseStocks(
            variantId ||
            undefined,
        );

    const warehouses =
        (
            stockState
                .warehouses
                .data ??
            []
        ).filter(
            (item) =>
                item.isActive,
        );

    useEffect(() => {
        if (
            !warehouseId &&
            warehouses[0]?.id
        ) {
            setWarehouseId(
                warehouses[0].id,
            );
        }
    }, [
        warehouseId,
        warehouses,
    ]);

    const locationQuery =
        useWarehouseLocations(
            warehouseId,
            false,
        );

    const locations =
        locationQuery.data ??
        [];

    useEffect(() => {
        if (
            !locationId &&
            locations[0]?.id
        ) {
            setLocationId(
                locations[0].id,
            );
        }
    }, [
        locationId,
        locations,
    ]);

    useEffect(() => {
        if (
            warehouseId &&
            !warehouses.some(
                (warehouse) =>
                    warehouse.id ===
                    warehouseId,
            )
        ) {
            setWarehouseId(
                warehouses[0]?.id ??
                '',
            );

            setLocationId('');
        }
    }, [
        warehouseId,
        warehouses,
    ]);

    useEffect(() => {
        if (
            locationId &&
            !locations.some(
                (location) =>
                    location.id ===
                    locationId,
            )
        ) {
            setLocationId(
                locations[0]?.id ??
                '',
            );
        }
    }, [
        locationId,
        locations,
    ]);

    const selectedStock =
        useMemo(
            () =>
                stockState.items.find(
                    (stock) =>
                        stock
                            .warehouseId ===
                        warehouseId &&
                        stock
                            .locationId ===
                        locationId &&
                        stock
                            .productVariantId ===
                        variantId,
                ),
            [
                stockState.items,
                warehouseId,
                locationId,
                variantId,
            ],
        );

    useEffect(() => {
        setOnHand(
            selectedStock
                ?.onHandQuantity ??
            0,
        );

        setReorderPoint(
            selectedStock
                ?.reorderPoint ??
            0,
        );
    }, [
        selectedStock?.id,
        selectedStock
            ?.onHandQuantity,
        selectedStock
            ?.reorderPoint,
    ]);

    const setStock =
        useSetWarehouseStock();

    const selectedVariant =
        variants.find(
            (variant) =>
                variant.id ===
                variantId,
        );

    const selectedLocation =
        locations.find(
            (location) =>
                location.id ===
                locationId,
        );

    const totalAvailable =
        stockState.items
            .filter(
                (stock) =>
                    stock
                        .productVariantId ===
                    variantId,
            )
            .reduce(
                (
                    sum,
                    stock,
                ) =>
                    sum +
                    stock.availableQuantity,
                0,
            );

    const movementQuery =
        useWarehouseStockMovements(
            warehouseId ||
            undefined,
            locationId ||
            undefined,
            variantId ||
            undefined,
            20,
        );

    if (
        productQuery.isLoading
    ) {
        return (
            <Card>
                <CardContent className="pt-6">
                    <div className="text-muted-foreground text-sm">
                        {t(
                            'inventory.loading',
                        )}
                    </div>
                </CardContent>
            </Card>
        );
    }

    if (
        productQuery.isError ||
        !productQuery.data
    ) {
        return (
            <Alert variant="destructive">
                <AlertDescription>
                    {t(
                        'inventory.productLoadError',
                    )}
                </AlertDescription>
            </Alert>
        );
    }

    if (
        warehouses.length === 0
    ) {
        return (
            <Card>
                <CardHeader>
                    <CardTitle>
                        {t(
                            'inventory.productInventoryTitle',
                        )}
                    </CardTitle>

                    <CardDescription>
                        {t(
                            'inventory.productInventoryDescription',
                        )}
                    </CardDescription>
                </CardHeader>

                <CardContent className="space-y-4">
                    <Alert>
                        <WarehouseIcon />

                        <AlertDescription>
                            <div className="space-y-1">
                                <div className="font-medium">
                                    {t(
                                        'inventory.noActiveWarehouses',
                                    )}
                                </div>

                                <div>
                                    {t(
                                        'inventory.noActiveWarehousesDescription',
                                    )}
                                </div>
                            </div>
                        </AlertDescription>
                    </Alert>

                    <Button asChild>
                        <Link to="/admin/warehouses/new">
                            <WarehouseIcon />
                            {t(
                                'inventory.goToWarehouses',
                            )}
                        </Link>
                    </Button>
                </CardContent>
            </Card>
        );
    }

    const save =
        async () => {
            if (
                !variantId ||
                !warehouseId ||
                !locationId
            ) {
                toast.error(
                    t(
                        'inventory.selectVariantWarehouseLocation',
                    ),
                );

                return;
            }

            const current =
                selectedStock;

            try {
                await setStock.mutateAsync({
                    warehouseId,
                    locationId,
                    productVariantId:
                        variantId,

                    onHandQuantity:
                        Math.max(
                            0,
                            Math.trunc(
                                onHand,
                            ),
                        ),

                    reservedQuantity:
                        current
                            ?.reservedQuantity ??
                        0,

                    incomingQuantity:
                        current
                            ?.incomingQuantity ??
                        0,

                    damagedQuantity:
                        current
                            ?.damagedQuantity ??
                        0,

                    reorderPoint:
                        Math.max(
                            0,
                            Math.trunc(
                                reorderPoint,
                            ),
                        ),
                });

                toast.success(
                    t(
                        'inventory.stockSaved',
                    ),
                );
            } catch (error) {
                toast.error(
                    error instanceof Error
                        ? error.message
                        : t(
                            'inventory.stockSaveError',
                        ),
                );
            }
        };

    const noActiveLocations =
        locationQuery.isSuccess &&
        locations.length === 0;

    return (
        <Card>
            <CardHeader>
                <div className="flex items-start gap-3">
                    <div className="rounded-lg border p-2">
                        <Boxes className="size-5" />
                    </div>

                    <div>
                        <CardTitle>
                            {t(
                                'inventory.productInventoryTitle',
                            )}
                        </CardTitle>

                        <CardDescription>
                            {t(
                                'inventory.productInventoryDescription',
                            )}
                        </CardDescription>
                    </div>
                </div>
            </CardHeader>

            <CardContent className="space-y-6">
                <Alert>
                    <Package />

                    <AlertDescription>
                        {t(
                            'inventory.singleProductRule',
                        )}
                    </AlertDescription>
                </Alert>

                {variants.length === 0 ? (
                    <Alert variant="destructive">
                        <AlertDescription>
                            {t(
                                'inventory.noActiveVariants',
                            )}
                        </AlertDescription>
                    </Alert>
                ) : (
                    <>
                        <div className="grid gap-4 md:grid-cols-3">
                            <div className="grid gap-2">
                                <Label htmlFor="inventory-variant">
                                    {t(
                                        'inventory.variant',
                                    )}
                                </Label>

                                <select
                                    id="inventory-variant"
                                    className="border-input bg-background h-10 w-full rounded-md border px-3 text-sm"
                                    value={variantId}
                                    onChange={(event) => {
                                        setVariantId(
                                            event.target.value,
                                        );

                                        setLocationId('');
                                    }}
                                >
                                    {variants.map(
                                        (
                                            variant: ProductVariant,
                                        ) => (
                                            <option
                                                key={variant.id}
                                                value={variant.id}
                                            >
                                                {variant.sku}
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

                            <div className="grid gap-2">
                                <Label htmlFor="inventory-warehouse">
                                    {t(
                                        'inventory.warehouse',
                                    )}
                                </Label>

                                <select
                                    id="inventory-warehouse"
                                    className="border-input bg-background h-10 w-full rounded-md border px-3 text-sm"
                                    value={warehouseId}
                                    onChange={(event) => {
                                        setWarehouseId(
                                            event.target.value,
                                        );

                                        setLocationId('');
                                    }}
                                >
                                    {warehouses.map(
                                        (
                                            warehouse,
                                        ) => (
                                            <option
                                                key={warehouse.id}
                                                value={warehouse.id}
                                            >
                                                {warehouse.code}
                                                {' — '}
                                                {warehouse.name}
                                            </option>
                                        ),
                                    )}
                                </select>
                            </div>

                            <div className="grid gap-2">
                                <Label htmlFor="inventory-location">
                                    {t(
                                        'inventory.location',
                                    )}
                                </Label>

                                <select
                                    id="inventory-location"
                                    className="border-input bg-background h-10 w-full rounded-md border px-3 text-sm"
                                    value={locationId}
                                    onChange={(event) =>
                                        setLocationId(
                                            event.target.value,
                                        )
                                    }
                                    disabled={
                                        locationQuery.isLoading ||
                                        noActiveLocations
                                    }
                                >
                                    {locations.map(
                                        (
                                            location,
                                        ) => (
                                            <option
                                                key={location.id}
                                                value={location.id}
                                            >
                                                {location.code}
                                                {' — '}
                                                {location.name}
                                            </option>
                                        ),
                                    )}
                                </select>
                            </div>
                        </div>

                        {noActiveLocations && (
                            <Alert>
                                <MapPin />

                                <AlertDescription className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
                                    <div className="space-y-1">
                                        <div className="font-medium">
                                            {t(
                                                'inventory.noActiveLocations',
                                            )}
                                        </div>

                                        <div>
                                            {t(
                                                'inventory.noActiveLocationsDescription',
                                            )}
                                        </div>
                                    </div>

                                    <Button
                                        asChild
                                        variant="outline"
                                    >
                                        <Link
                                            to={`/admin/warehouses/${warehouseId}/edit`}
                                        >
                                            <MapPin />
                                            {t(
                                                'inventory.goToWarehouse',
                                            )}
                                        </Link>
                                    </Button>
                                </AlertDescription>
                            </Alert>
                        )}

                        <div className="rounded-lg border p-4">
                            <div className="mb-4 flex flex-wrap items-center justify-between gap-3">
                                <div>
                                    <div className="font-medium">
                                        {selectedVariant?.sku}
                                    </div>

                                    <div className="text-muted-foreground text-sm">
                                        {productQuery.data.name}
                                    </div>
                                </div>

                                <Badge
                                    variant={
                                        totalAvailable > 0
                                            ? 'default'
                                            : 'secondary'
                                    }
                                >
                                    {t(
                                        'inventory.totalAvailable',
                                    )}
                                    {': '}
                                    {totalAvailable}
                                </Badge>
                            </div>

                            <div className="grid gap-4 md:grid-cols-2">
                                <div className="grid gap-2">
                                    <Label htmlFor="inventory-on-hand">
                                        {t(
                                            'inventory.onHand',
                                        )}
                                    </Label>

                                    <Input
                                        id="inventory-on-hand"
                                        type="number"
                                        min={0}
                                        value={onHand}
                                        onChange={(event) =>
                                            setOnHand(
                                                Number(
                                                    event.target.value,
                                                ) || 0,
                                            )
                                        }
                                        disabled={
                                            noActiveLocations
                                        }
                                    />
                                </div>

                                <div className="grid gap-2">
                                    <Label htmlFor="inventory-reorder">
                                        {t(
                                            'inventory.reorderPoint',
                                        )}
                                    </Label>

                                    <Input
                                        id="inventory-reorder"
                                        type="number"
                                        min={0}
                                        value={reorderPoint}
                                        onChange={(event) =>
                                            setReorderPoint(
                                                Number(
                                                    event.target.value,
                                                ) || 0,
                                            )
                                        }
                                        disabled={
                                            noActiveLocations
                                        }
                                    />
                                </div>
                            </div>

                            <div className="mt-4 flex justify-end">
                                <Button
                                    onClick={save}
                                    disabled={
                                        setStock.isPending ||
                                        !locations.length
                                    }
                                >
                                    {t(
                                        'inventory.saveStock',
                                    )}
                                </Button>
                            </div>
                        </div>

                        <div>
                            <div className="mb-3 font-medium">
                                {t(
                                    'inventory.currentLocations',
                                )}
                            </div>

                            {stockState.isLoading ? (
                                <div className="text-muted-foreground text-sm">
                                    {t(
                                        'inventory.loading',
                                    )}
                                </div>
                            ) : stockState.items.length === 0 ? (
                                <div className="rounded-lg border border-dashed p-5 text-sm text-muted-foreground">
                                    {t(
                                        'inventory.noStockLocations',
                                    )}
                                </div>
                            ) : (
                                <div className="grid gap-3 md:grid-cols-2 xl:grid-cols-3">
                                    {stockState.items.map(
                                        (stock) => {
                                            const warehouse =
                                                warehouses.find(
                                                    (
                                                        item,
                                                    ) =>
                                                        item.id ===
                                                        stock.warehouseId,
                                                );

                                            const location =
                                                locations.find(
                                                    (
                                                        item,
                                                    ) =>
                                                        item.id ===
                                                        stock.locationId,
                                                );

                                            return (
                                                <button
                                                    key={stock.id}
                                                    type="button"
                                                    className="rounded-lg border p-4 text-start transition hover:bg-muted/40"
                                                    onClick={() => {
                                                        setWarehouseId(
                                                            stock.warehouseId,
                                                        );

                                                        setLocationId(
                                                            stock.locationId,
                                                        );
                                                    }}
                                                >
                                                    <div className="flex items-center gap-2 font-medium">
                                                        <WarehouseIcon className="size-4" />

                                                        {
                                                            warehouse?.name ??
                                                            stock.warehouseId
                                                        }
                                                    </div>

                                                    <div className="text-muted-foreground mt-1 flex items-center gap-2 text-sm">
                                                        <MapPin className="size-4" />

                                                        {
                                                            location?.name ??
                                                            stock.locationId
                                                        }
                                                    </div>

                                                    <div className="mt-3 grid grid-cols-3 gap-2 text-sm">
                                                        <Metric
                                                            label={t(
                                                                'inventory.onHandShort',
                                                            )}
                                                            value={
                                                                stock.onHandQuantity
                                                            }
                                                        />

                                                        <Metric
                                                            label={t(
                                                                'inventory.reservedShort',
                                                            )}
                                                            value={
                                                                stock.reservedQuantity
                                                            }
                                                        />

                                                        <Metric
                                                            label={t(
                                                                'inventory.availableShort',
                                                            )}
                                                            value={
                                                                stock.availableQuantity
                                                            }
                                                        />
                                                    </div>
                                                </button>
                                            );
                                        },
                                    )}
                                </div>
                            )}
                        </div>

                        <div className="space-y-3">
                            <div className="flex flex-wrap items-center justify-between gap-3">
                                <div>
                                    <div className="font-medium">
                                        {t(
                                            'inventory.movementLedgerTitle',
                                        )}
                                    </div>

                                    <div className="text-muted-foreground text-sm">
                                        {t(
                                            'inventory.movementLedgerDescription',
                                        )}
                                    </div>
                                </div>

                                {selectedLocation && (
                                    <Badge variant="outline">
                                        {
                                            selectedLocation.code
                                        }
                                    </Badge>
                                )}
                            </div>

                            {movementQuery.isLoading ? (
                                <div className="rounded-lg border p-5 text-sm text-muted-foreground">
                                    {t(
                                        'inventory.loading',
                                    )}
                                </div>
                            ) : movementQuery.isError ? (
                                <Alert variant="destructive">
                                    <AlertDescription>
                                        {t(
                                            'inventory.movementLoadError',
                                        )}
                                    </AlertDescription>
                                </Alert>
                                    ) : (movementQuery.data ?? []).length === 0 ? (
                                <div className="rounded-lg border border-dashed p-5 text-sm text-muted-foreground">
                                    {t(
                                        'inventory.noMovements',
                                    )}
                                </div>
                            ) : (
                                <div className="overflow-x-auto rounded-lg border">
                                    <table className="w-full text-sm">
                                        <thead>
                                            <tr className="border-b bg-muted/30">
                                                <th className="px-3 py-3 text-start">
                                                    {t(
                                                        'inventory.movementDate',
                                                    )}
                                                </th>

                                                <th className="px-3 py-3 text-start">
                                                    {t(
                                                        'inventory.movementType',
                                                    )}
                                                </th>

                                                <th className="px-3 py-3 text-start">
                                                    {t(
                                                        'inventory.movementDelta',
                                                    )}
                                                </th>

                                                <th className="px-3 py-3 text-start">
                                                    {t(
                                                        'inventory.movementBalance',
                                                    )}
                                                </th>

                                                <th className="px-3 py-3 text-start">
                                                    {t(
                                                        'inventory.movementReason',
                                                    )}
                                                </th>
                                            </tr>
                                        </thead>

                                        <tbody>
                                                            {(movementQuery.data ?? []).map(
                                                (
                                                    movement,
                                                ) => (
                                                    <tr
                                                        key={
                                                            movement.id
                                                        }
                                                        className="border-b last:border-0"
                                                    >
                                                        <td className="whitespace-nowrap px-3 py-3">
                                                            {formatDate(
                                                                movement.occurredAt,
                                                            )}
                                                        </td>

                                                        <td className="px-3 py-3">
                                                            <Badge variant="outline">
                                                                {t(
                                                                    `inventory.movementTypes.${movement.type}`,
                                                                    {
                                                                        defaultValue:
                                                                            movement.type,
                                                                    },
                                                                )}
                                                            </Badge>
                                                        </td>

                                                        <td className="px-3 py-3">
                                                            <Badge
                                                                variant={
                                                                    movement.quantityDelta >
                                                                        0
                                                                        ? 'default'
                                                                        : 'destructive'
                                                                }
                                                            >
                                                                {movement.quantityDelta >
                                                                    0
                                                                    ? '+'
                                                                    : ''}
                                                                {
                                                                    movement.quantityDelta
                                                                }
                                                            </Badge>
                                                        </td>

                                                        <td className="px-3 py-3 font-medium">
                                                            {
                                                                movement.balanceAfter
                                                            }
                                                        </td>

                                                        <td className="max-w-72 px-3 py-3 text-muted-foreground">
                                                            {
                                                                movement.reason ??
                                                                '—'
                                                            }
                                                        </td>
                                                    </tr>
                                                ),
                                            )}
                                        </tbody>
                                    </table>
                                </div>
                            )}
                        </div>
                    </>
                )}
            </CardContent>
        </Card>
    );
}

function Metric({
    label,
    value,
}: {
    label: string;
    value: number;
}) {
    return (
        <div>
            <div className="text-muted-foreground text-xs">
                {label}
            </div>

            <div className="font-semibold">
                {value}
            </div>
        </div>
    );
}

function formatDate(
    value: string,
) {
    try {
        return new Intl.DateTimeFormat(
            undefined,
            {
                dateStyle: 'short',
                timeStyle: 'short',
            },
        ).format(
            new Date(value),
        );
    } catch {
        return value;
    }
}