// src/modules/catalog/api/categories.ts
import { api } from '@/lib/api/client';

export type Category = {
    id: string;
    name: string;
    slug?: string;
    description?: string;
    imageUrl?: string;
    parentCategoryId?: string | null;
    parentCategoryName?: string;
    displayOrder: number;
    isActive: boolean;
    isPublished: boolean;
    isFeatured: boolean;
    productCount?: number;
    createdAt: string;
    updatedAt?: string;
    subCategories?: Category[];
};

export type CategoryFilter = {
    page?: number;
    pageSize?: number;
    search?: string;
    isActive?: boolean;
    isPublished?: boolean;
    isFeatured?: boolean;
    parentCategoryId?: string;
    sortBy?: string;
    desc?: boolean;
};

export type CreateCategoryDto = {
    name: string;
    slug?: string;
    description?: string;
    imageUrl?: string;
    parentCategoryId?: string | null;
    displayOrder?: number;
    isActive?: boolean;
    isPublished?: boolean;
    isFeatured?: boolean;
};

export type UpdateCategoryDto = {
    name: string;
    slug?: string;
    description?: string;
    imageUrl?: string;
    parentCategoryId?: string | null;
    displayOrder?: number;
    isActive?: boolean;
    isPublished?: boolean;
    isFeatured?: boolean;
};

export const categoriesApi = {
    // ✅ حذف /api اضافی - فقط از مسیر نسبی استفاده کنید
    getAll: (params?: CategoryFilter) =>
        api.get<Category[]>('/categories', { params }), // ✅ بدون /api

    getRoots: () =>
        api.get<Category[]>('/categories/roots'), // ✅ بدون /api

    getById: (id: string) =>
        api.get<Category>(`/categories/${id}`), // ✅ بدون /api

    getBySlug: (slug: string) =>
        api.get<Category>(`/categories/slug/${slug}`), // ✅ بدون /api

    getChildren: (parentId: string) =>
        api.get<Category[]>(`/categories/${parentId}/children`), // ✅ بدون /api

    create: (data: CreateCategoryDto) =>
        api.post<Category>('/categories', data), // ✅ بدون /api

    update: (id: string, data: UpdateCategoryDto) =>
        api.put<Category>(`/categories/${id}`, data), // ✅ بدون /api

    delete: (id: string) =>
        api.del<void>(`/categories/${id}`), // ✅ بدون /api
};