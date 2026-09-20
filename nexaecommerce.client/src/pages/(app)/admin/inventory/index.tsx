import { useMemo, useState } from 'react';
import { Link } from 'react-router';
import { Boxes, Package, Search, Warehouse as WarehouseIcon } from 'lucide-react';
import { useTranslation } from 'react-i18next';

import { PageHeader, EmptyState, ErrorState, LoadingSkeleton } from '@/components/data-states';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Badge } from '@/components/ui/badge';
import { usePermission } from '@/hooks/use-permission';
import { INVENTORY_PERM } from '@/lib/api/inventory';
import { useAdminProducts } from '@/modules/catalog/products/hooks';
import ProductInventoryManager from '@/modules/inventory/components/ProductInventoryManager';
import { useDocumentTitle } from '@/hooks/use-document-title';

export default function InventoryPage() {
    const { t } = useTranslation();
    useDocumentTitle(t('inventory.title'));

    const canManage = usePermission(INVENTORY_PERM.manage);
    const [search, setSearch] = useState('');
    const [selectedProductId, setSelectedProductId] = useState<string | null>(null);

    const query = useAdminProducts({ page: 1, pageSize: 100, search, sortBy: 'newest' });
    const products = useMemo(() => query.data?.items ?? [], [query.data]);
    const selectedProduct = products.find((product) => product.id === selectedProductId);

    return (
        <div className="space-y-6">
            <PageHeader
                title={t('inventory.title')}
                description={t('inventory.description')}
                actions={(
                    <div className="flex flex-wrap gap-2">
                        <Button asChild variant="outline">
                            <Link to="/admin/warehouses">
                                <WarehouseIcon />
                                {t('inventory.warehouses')}
                            </Link>
                        </Button>
                        <Button asChild>
                            <Link to="/admin/products/new">
                                <Package />
                                {t('inventory.catalogProduct')}
                            </Link>
                        </Button>
                    </div>
                )}
            />

            <Card>
                <CardContent className="space-y-4 pt-6">
                    <div className="flex flex-col gap-3 md:flex-row md:items-center md:justify-between">
                        <div className="relative w-full md:max-w-md">
                            <Search className="text-muted-foreground absolute start-3 top-1/2 size-4 -translate-y-1/2" />
                            <Input
                                className="ps-9"
                                value={search}
                                onChange={(event) => {
                                    setSearch(event.target.value);
                                    setSelectedProductId(null);
                                }}
                                placeholder={t('inventory.searchPlaceholder')}
                            />
                        </div>
                        <Badge variant="outline">{t('inventory.catalogOnly')}</Badge>
                    </div>

                    {query.isLoading && <LoadingSkeleton variant="table" rows={5} cols={4} />}
                    {query.isError && (
                        <ErrorState
                            error={query.error}
                            onRetry={() => query.refetch()}
                            message={t('inventory.productLoadError')}
                        />
                    )}
                    {query.isSuccess && products.length === 0 && (
                        <EmptyState
                            icon={Boxes}
                            title={t('inventory.noProductsTitle')}
                            description={search ? t('inventory.searchNoProductsDescription') : t('inventory.noProductsDescription')}
                            action={(
                                <Button asChild>
                                    <Link to="/admin/products/new">
                                        <Package />
                                        {t('inventory.registerProduct')}
                                    </Link>
                                </Button>
                            )}
                        />
                    )}

                    {query.isSuccess && products.length > 0 && (
                        <div className="grid gap-3 md:grid-cols-2 xl:grid-cols-3">
                            {products.map((product) => (
                                <button
                                    key={product.id}
                                    type="button"
                                    className={`rounded-lg border p-4 text-start transition hover:bg-muted/40 ${selectedProductId === product.id ? 'border-primary' : ''}`}
                                    onClick={() => setSelectedProductId(product.id)}
                                >
                                    <div className="flex items-center gap-3">
                                        <div className="bg-muted grid size-11 shrink-0 place-items-center overflow-hidden rounded-lg border">
                                            {product.mainImage ? (
                                                <img src={product.mainImage} alt="" className="size-full object-contain" />
                                            ) : (
                                                <Package className="text-muted-foreground size-5" />
                                            )}
                                        </div>
                                        <div className="min-w-0">
                                            <div className="truncate font-medium">{product.name}</div>
                                            <div className="text-muted-foreground text-xs">SKU: {product.sku || '—'}</div>
                                        </div>
                                    </div>
                                    <div className="mt-4 flex items-center justify-between gap-2 text-sm">
                                        <span className="text-muted-foreground">{t('inventory.activeInventory')}</span>
                                        <Badge variant={product.isInStock ? 'default' : 'secondary'}>{product.stockQuantity}</Badge>
                                    </div>
                                </button>
                            ))}
                        </div>
                    )}
                </CardContent>
            </Card>

            {selectedProductId && selectedProduct && canManage && (
                <ProductInventoryManager productId={selectedProductId} />
            )}

            {selectedProductId && !canManage && (
                <Card>
                    <CardContent className="pt-6 text-sm text-muted-foreground">
                        {t('inventory.noManagePermission')}
                    </CardContent>
                </Card>
            )}
        </div>
    );
}
