/* eslint-disable react-hooks/set-state-in-effect */

import { useEffect, useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Boxes, MapPin, Package, Warehouse as WarehouseIcon } from 'lucide-react';
import { toast } from 'sonner';

import { Alert, AlertDescription } from '@/components/ui/alert';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { useProduct } from '@/modules/catalog/products/hooks';
import { useProductWarehouseStocks, useSetWarehouseStock } from '@/modules/inventory/hooks/useInventoryStock';
import { useWarehouseLocations } from '@/modules/inventory/hooks/useWarehouses';
import type { ProductVariant } from '@/modules/catalog/api/products';

export default function ProductInventoryManager({ productId }: { productId: string }) {
    const { t } = useTranslation();
    const productQuery = useProduct(productId);
    const [variantId, setVariantId] = useState('');
    const [warehouseId, setWarehouseId] = useState('');
    const [locationId, setLocationId] = useState('');
    const [onHand, setOnHand] = useState(0);
    const [reorderPoint, setReorderPoint] = useState(0);

    const variants = useMemo(
        () => (productQuery.data?.variants ?? []).filter((variant) => variant.isActive),
        [productQuery.data?.variants],
    );

    useEffect(() => {
        if (!variantId && variants[0]?.id) {
            setVariantId(variants[0].id);
        }
    }, [variantId, variants]);

    const stockState = useProductWarehouseStocks(variantId || undefined);
    const warehouses = (stockState.warehouses.data ?? []).filter((item) => item.isActive);

    useEffect(() => {
        if (!warehouseId && warehouses[0]?.id) {
            setWarehouseId(warehouses[0].id);
        }
    }, [warehouseId, warehouses]);

    const locationQuery = useWarehouseLocations(warehouseId, false);
    const locations = locationQuery.data ?? [];

    useEffect(() => {
        if (!locationId && locations[0]?.id) {
            setLocationId(locations[0].id);
        }
    }, [locationId, locations]);

    const selectedStock = useMemo(
        () =>
            stockState.items.find(
                (stock) =>
                    stock.warehouseId === warehouseId &&
                    stock.locationId === locationId &&
                    stock.productVariantId === variantId,
            ),
        [stockState.items, warehouseId, locationId, variantId],
    );

    useEffect(() => {
        setOnHand(selectedStock?.onHandQuantity ?? 0);
        setReorderPoint(selectedStock?.reorderPoint ?? 0);
    }, [selectedStock?.id, selectedStock?.onHandQuantity, selectedStock?.reorderPoint]);

    const setStock = useSetWarehouseStock();
    const selectedVariant = variants.find((variant) => variant.id === variantId);
    const totalAvailable = stockState.items
        .filter((stock) => stock.productVariantId === variantId)
        .reduce((sum, stock) => sum + stock.availableQuantity, 0);

    if (productQuery.isLoading) {
        return <div className="text-muted-foreground text-sm">{t('inventory.loading')}</div>;
    }

    if (productQuery.isError || !productQuery.data) {
        return <Alert variant="destructive"><AlertDescription>{t('inventory.productLoadError')}</AlertDescription></Alert>;
    }

    const save = async () => {
        if (!variantId || !warehouseId || !locationId) {
            toast.error(t('inventory.selectVariantWarehouseLocation'));
            return;
        }

        const current = selectedStock;
        try {
            await setStock.mutateAsync({
                warehouseId,
                locationId,
                productVariantId: variantId,
                onHandQuantity: Math.max(0, Math.trunc(onHand)),
                reservedQuantity: current?.reservedQuantity ?? 0,
                incomingQuantity: current?.incomingQuantity ?? 0,
                damagedQuantity: current?.damagedQuantity ?? 0,
                reorderPoint: Math.max(0, Math.trunc(reorderPoint)),
            });
            toast.success(t('inventory.stockSaved'));
        } catch (error) {
            toast.error(error instanceof Error ? error.message : t('inventory.stockSaveError'));
        }
    };

    return (
        <Card>
            <CardHeader>
                <div className="flex items-start gap-3">
                    <div className="rounded-lg border p-2"><Boxes className="size-5" /></div>
                    <div>
                        <CardTitle>{t('inventory.productInventoryTitle')}</CardTitle>
                        <CardDescription>{t('inventory.productInventoryDescription')}</CardDescription>
                    </div>
                </div>
            </CardHeader>

            <CardContent className="space-y-6">
                <Alert>
                    <Package />
                    <AlertDescription>{t('inventory.singleProductRule')}</AlertDescription>
                </Alert>

                {variants.length === 0 ? (
                    <Alert variant="destructive"><AlertDescription>{t('inventory.noActiveVariants')}</AlertDescription></Alert>
                ) : (
                    <>
                        <div className="grid gap-4 md:grid-cols-3">
                            <div className="grid gap-2">
                                <Label htmlFor="inventory-variant">{t('inventory.variant')}</Label>
                                <select id="inventory-variant" className="border-input bg-background h-10 w-full rounded-md border px-3 text-sm" value={variantId} onChange={(event) => { setVariantId(event.target.value); setLocationId(''); }}>
                                    {variants.map((variant: ProductVariant) => (
                                        <option key={variant.id} value={variant.id}>{variant.sku}{variant.color ? ` — ${variant.color}` : ''}{variant.size ? ` — ${variant.size}` : ''}</option>
                                    ))}
                                </select>
                            </div>

                            <div className="grid gap-2">
                                <Label htmlFor="inventory-warehouse">{t('inventory.warehouse')}</Label>
                                <select id="inventory-warehouse" className="border-input bg-background h-10 w-full rounded-md border px-3 text-sm" value={warehouseId} onChange={(event) => { setWarehouseId(event.target.value); setLocationId(''); }}>
                                    {warehouses.map((warehouse) => (
                                        <option key={warehouse.id} value={warehouse.id}>{warehouse.code} — {warehouse.name}</option>
                                    ))}
                                </select>
                            </div>

                            <div className="grid gap-2">
                                <Label htmlFor="inventory-location">{t('inventory.location')}</Label>
                                <select id="inventory-location" className="border-input bg-background h-10 w-full rounded-md border px-3 text-sm" value={locationId} onChange={(event) => setLocationId(event.target.value)}>
                                    {locations.map((location) => (
                                        <option key={location.id} value={location.id}>{location.code} — {location.name}</option>
                                    ))}
                                </select>
                            </div>
                        </div>

                        <div className="rounded-lg border p-4">
                            <div className="mb-4 flex flex-wrap items-center justify-between gap-3">
                                <div>
                                    <div className="font-medium">{selectedVariant?.sku}</div>
                                    <div className="text-muted-foreground text-sm">{productQuery.data.name}</div>
                                </div>
                                <Badge variant={totalAvailable > 0 ? 'default' : 'secondary'}>
                                    {t('inventory.totalAvailable')}: {totalAvailable}
                                </Badge>
                            </div>

                            <div className="grid gap-4 md:grid-cols-2">
                                <div className="grid gap-2">
                                    <Label htmlFor="inventory-on-hand">{t('inventory.onHand')}</Label>
                                    <Input id="inventory-on-hand" type="number" min={0} value={onHand} onChange={(event) => setOnHand(Number(event.target.value) || 0)} />
                                </div>
                                <div className="grid gap-2">
                                    <Label htmlFor="inventory-reorder">{t('inventory.reorderPoint')}</Label>
                                    <Input id="inventory-reorder" type="number" min={0} value={reorderPoint} onChange={(event) => setReorderPoint(Number(event.target.value) || 0)} />
                                </div>
                            </div>

                            <div className="mt-4 flex justify-end">
                                <Button onClick={save} disabled={setStock.isPending || !locations.length || !warehouses.length}>
                                    {t('inventory.saveStock')}
                                </Button>
                            </div>
                        </div>

                        <div>
                            <div className="mb-3 font-medium">{t('inventory.currentLocations')}</div>
                            {stockState.isLoading ? (
                                <div className="text-muted-foreground text-sm">{t('inventory.loading')}</div>
                            ) : stockState.items.length === 0 ? (
                                <div className="rounded-lg border border-dashed p-5 text-sm text-muted-foreground">{t('inventory.noStockLocations')}</div>
                            ) : (
                                <div className="grid gap-3 md:grid-cols-2 xl:grid-cols-3">
                                    {stockState.items.map((stock) => {
                                        const warehouse = warehouses.find((item) => item.id === stock.warehouseId);
                                        return (
                                            <button key={stock.id} type="button" className="rounded-lg border p-4 text-start transition hover:bg-muted/40" onClick={() => { setWarehouseId(stock.warehouseId); setLocationId(stock.locationId); }}>
                                                <div className="flex items-center gap-2 font-medium"><WarehouseIcon className="size-4" />{warehouse?.name ?? stock.warehouseId}</div>
                                                <div className="text-muted-foreground mt-1 flex items-center gap-2 text-sm"><MapPin className="size-4" />{stock.locationId}</div>
                                                <div className="mt-3 grid grid-cols-3 gap-2 text-sm">
                                                    <Metric label={t('inventory.onHandShort')} value={stock.onHandQuantity} />
                                                    <Metric label={t('inventory.reservedShort')} value={stock.reservedQuantity} />
                                                    <Metric label={t('inventory.availableShort')} value={stock.availableQuantity} />
                                                </div>
                                            </button>
                                        );
                                    })}
                                </div>
                            )}
                        </div>
                    </>
                )}
            </CardContent>
        </Card>
    );
}

function Metric({ label, value }: { label: string; value: number }) {
    return <div><div className="text-muted-foreground text-xs">{label}</div><div className="font-semibold">{value}</div></div>;
}
