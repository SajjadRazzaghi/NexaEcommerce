import {
    useMutation,
    useQueries,
    useQuery,
    useQueryClient,
} from '@tanstack/react-query';

import {
    warehouseStockApi,
    type WarehouseStock,
    type Warehouse,
} from '@/lib/api/inventory';
import { useWarehouses } from './useWarehouses';

export const inventoryStockQueryKeys = {
    all: ['inventory', 'warehouse-stock'] as const,
    warehouse: (warehouseId: string, productVariantId?: string) =>
        ['inventory', 'warehouse-stock', warehouseId, productVariantId ?? 'all'] as const,
};

export function useWarehouseStock(
    warehouseId?: string,
    productVariantId?: string,
) {
    return useQuery({
        queryKey: warehouseId
            ? inventoryStockQueryKeys.warehouse(warehouseId, productVariantId)
            : ['inventory', 'warehouse-stock', 'empty'],
        queryFn: async () => {
            if (!warehouseId) return [] as WarehouseStock[];
            return (
                await warehouseStockApi.listByWarehouse(
                    warehouseId,
                    productVariantId,
                    true,
                )
            ).items;
        },
        enabled: Boolean(warehouseId),
    });
}

export function useProductWarehouseStocks(
    productVariantId?: string,
) {
    const warehouses = useWarehouses(false);

    const queries = useQueries({
        queries: (warehouses.data ?? []).map((warehouse: Warehouse) => ({
            queryKey: inventoryStockQueryKeys.warehouse(
                warehouse.id,
                productVariantId,
            ),
            queryFn: async () =>
                (
                    await warehouseStockApi.listByWarehouse(
                        warehouse.id,
                        productVariantId,
                        true,
                    )
                ).items,
            enabled: Boolean(productVariantId),
        })),
    });

    const items = queries.flatMap((query) => query.data ?? []);

    return {
        warehouses,
        queries,
        items,
        isLoading:
            warehouses.isLoading ||
            queries.some((query) => query.isLoading),
        isError:
            warehouses.isError ||
            queries.some((query) => query.isError),
        refetch: async () => {
            await warehouses.refetch();
            await Promise.all(queries.map((query) => query.refetch()));
        },
    };
}

export function useSetWarehouseStock() {
    const queryClient = useQueryClient();

    return useMutation({
        mutationFn: warehouseStockApi.set,
        onSuccess: (stock) => {
            queryClient.invalidateQueries({
                queryKey: inventoryStockQueryKeys.all,
            });
            queryClient.invalidateQueries({
                queryKey: inventoryStockQueryKeys.warehouse(
                    stock.warehouseId,
                    stock.productVariantId,
                ),
            });
            queryClient.invalidateQueries({
                queryKey: ['products'],
            });
        },
    });
}
