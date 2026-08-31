import { useQuery } from '@tanstack/react-query';

import {
    categoriesApi,
} from '../api/categories';

export function useCategories() {
    return useQuery({
        queryKey: [
            'categories',
        ],
        queryFn: async () => {
            const response =
                await categoriesApi.getAll({
                    page: 1,
                    pageSize: 100,
                    isActive: true,
                    isPublished: true,
                    sortBy: 'displayOrder',
                    desc: false,
                });

            return response;
        },
        staleTime: 5 * 60_000,
    });
}

export function useCategory(
    id?: string,
) {
    return useQuery({
        queryKey: [
            'category',
            id,
        ],
        queryFn: async () => {
            const response =
                await categoriesApi.getById(
                    id!,
                );

            return response;
        },
        enabled: Boolean(id),
        staleTime: 5 * 60_000,
    });
}