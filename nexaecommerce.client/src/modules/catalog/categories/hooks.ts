// src/modules/catalog/categories/hooks.ts
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { categoriesApi } from "@/modules/catalog/api/categories";
import type { CategoryFilter, CreateCategoryDto, UpdateCategoryDto } from '@/modules/catalog/api/categories';
import { api } from '@/lib/api/client';

const CATEGORIES_KEY = 'categories';

// دریافت همه دسته‌بندی‌ها
export const useCategories = (params?: CategoryFilter) => {
    return useQuery({
        queryKey: [CATEGORIES_KEY, params],
        queryFn: async () => {
            return categoriesApi.getAll(params);
        },
    });
};

// دریافت دسته‌بندی‌های ریشه
export const useRootCategories = () => {
    return useQuery({
        queryKey: [CATEGORIES_KEY, 'roots'],
        queryFn: async () => {
            return categoriesApi.getRoots();
        },
    });
};

// دریافت یک دسته‌بندی
export const useCategory = (id?: string) => {
    return useQuery({
        queryKey: [CATEGORIES_KEY, id],
        queryFn: async () => {
            if (!id) throw new Error('ID is required');
            return categoriesApi.getById(id);
        },
        enabled: !!id,
    });
};

// دریافت زیردسته‌ها
export const useCategoryChildren = (parentId: string) => {
    return useQuery({
        queryKey: [CATEGORIES_KEY, parentId, 'children'],
        queryFn: async () => {
            return categoriesApi.getChildren(parentId);
        },
        enabled: !!parentId,
    });
};

// ایجاد دسته‌بندی
export const useCreateCategory = () => {
    const queryClient = useQueryClient();

    return useMutation({
        mutationFn: async (data: CreateCategoryDto) => {
            return categoriesApi.create(data);
        },
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: [CATEGORIES_KEY] });
        },
    });
};

// بروزرسانی دسته‌بندی
export const useUpdateCategory = () => {
    const queryClient = useQueryClient();

    return useMutation({
        mutationFn: async ({ id, data }: { id: string; data: UpdateCategoryDto }) => {
            return categoriesApi.update(id, data);
        },
        onSuccess: (_, variables) => {
            queryClient.invalidateQueries({ queryKey: [CATEGORIES_KEY] });
            queryClient.invalidateQueries({ queryKey: [CATEGORIES_KEY, variables.id] });
        },
    });
};

// حذف دسته‌بندی
export const useDeleteCategory = () => {
    const queryClient = useQueryClient();

    return useMutation({
        mutationFn: async (id: string) => {
            await categoriesApi.delete(id);
        },
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: [CATEGORIES_KEY] });
        },
    });
};

// اکشن‌های دسته‌بندی
export const useCategoryAction = (action: 'activate' | 'deactivate' | 'publish' | 'unpublish' | 'feature' | 'unfeature') => {
    const queryClient = useQueryClient();

    return useMutation({
        mutationFn: async (id: string) =>
            api.patch<void>(`/categories/${id}/${action}`),
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: [CATEGORIES_KEY] });
        },
    });
};