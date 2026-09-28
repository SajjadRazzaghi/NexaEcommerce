// src/modules/catalog/api/categories.ts

import { api } from '@/lib/api/client';

export type Category = {
    id: string;

    name: string;

    slug?: string | null;

    description?: string | null;

    imageUrl?: string | null;

    // =========================================================
    // SEO
    // =========================================================

    seoTitle?: string | null;

    seoDescription?: string | null;

    seoKeywords?: string | null;

    // =========================================================
    // Hierarchy
    // =========================================================

    parentCategoryId?: string | null;

    parentCategoryName?: string | null;

    // =========================================================
    // Display / Status
    // =========================================================

    displayOrder: number;

    isActive: boolean;

    isPublished: boolean;

    isFeatured: boolean;

    // =========================================================
    // Statistics / Audit
    // =========================================================

    productCount?: number;

    createdAt: string;

    updatedAt?: string | null;

    // =========================================================
    // Children
    // =========================================================

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

// =========================================================
// Create
// =========================================================

export type CreateCategoryDto = {
    name: string;

    slug?: string | null;

    description?: string | null;

    imageUrl?: string | null;

    // SEO
    seoTitle?: string | null;

    seoDescription?: string | null;

    seoKeywords?: string | null;

    // Hierarchy
    parentCategoryId?: string | null;

    // Display / Status
    displayOrder?: number;

    isActive?: boolean;

    isPublished?: boolean;

    isFeatured?: boolean;
};

// =========================================================
// Update
// =========================================================

export type UpdateCategoryDto = {
    name: string;

    slug?: string | null;

    description?: string | null;

    imageUrl?: string | null;

    // SEO
    seoTitle?: string | null;

    seoDescription?: string | null;

    seoKeywords?: string | null;

    // Hierarchy
    parentCategoryId?: string | null;

    // Display / Status
    displayOrder?: number;

    isActive?: boolean;

    isPublished?: boolean;

    isFeatured?: boolean;
};

// =========================================================
// API
// =========================================================

export const categoriesApi = {
    // ---------------------------------------------------------
    // List
    // ---------------------------------------------------------

    getAll: (params?: CategoryFilter) =>
        api.get<Category[]>('/categories', {
            params,
        }),

    // ---------------------------------------------------------
    // Root Categories
    // ---------------------------------------------------------

    getRoots: () =>
        api.get<Category[]>('/categories/roots'),

    // ---------------------------------------------------------
    // Get By Id
    // ---------------------------------------------------------

    getById: (id: string) =>
        api.get<Category>(
            `/categories/${id}`,
        ),

    // ---------------------------------------------------------
    // Get By Slug
    // ---------------------------------------------------------

    getBySlug: (slug: string) =>
        api.get<Category>(
            `/categories/slug/${encodeURIComponent(slug)}`,
        ),

    // ---------------------------------------------------------
    // Get Children
    // ---------------------------------------------------------

    getChildren: (parentId: string) =>
        api.get<Category[]>(
            `/categories/${parentId}/children`,
        ),

    // ---------------------------------------------------------
    // Create
    // ---------------------------------------------------------

    create: (data: CreateCategoryDto) =>
        api.post<Category>(
            '/categories',
            data,
        ),

    // ---------------------------------------------------------
    // Update
    // ---------------------------------------------------------

    // Backend returns 204 No Content.
    update: (id: string, data: UpdateCategoryDto) =>
        api.put<void>(
            `/categories/${id}`,
            data,
        ),

    // ---------------------------------------------------------
    // Delete
    // ---------------------------------------------------------

    // Backend returns 204 No Content.
    delete: (id: string) =>
        api.del<void>(
            `/categories/${id}`,
        ),
};