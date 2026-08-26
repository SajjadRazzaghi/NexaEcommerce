import { api } from '@/lib/api/client';
import type { PagedResult } from '@/lib/api/paged';

export interface BrandListItem {
  id: string;
  name: string;
  slug: string;
  logoUrl?: string | null;
  isActive: boolean;
  isPublished: boolean;
  isFeatured: boolean;
  displayOrder: number;
}

export interface BrandDetails extends BrandListItem {
  description?: string | null;
  website?: string | null;
  coverImageUrl?: string | null;
  seoTitle?: string | null;
  seoDescription?: string | null;
  seoKeywords?: string | null;
  createdAt: string;
  updatedAt?: string | null;
}

export interface BrandLookupItem {
  id: string;
  name: string;
}

export interface BrandFilter {
  page?: number;
  pageSize?: number;
  search?: string;
  isActive?: boolean;
  isPublished?: boolean;
  isFeatured?: boolean;
  sortBy?: string;
  desc?: boolean;
}

export interface CreateBrandDto {
  name: string;
  description?: string | null;
  website?: string | null;
  logoUrl?: string | null;
  coverImageUrl?: string | null;
  seoTitle?: string | null;
  seoDescription?: string | null;
  seoKeywords?: string | null;
}

export interface UpdateBrandDto extends CreateBrandDto {
  slug?: string | null;
  isActive: boolean;
  isPublished: boolean;
  isFeatured: boolean;
  displayOrder: number;
}

export interface CreateBrandResponse {
  id: string;
}

function buildQuery(filter: BrandFilter = {}) {
  const params = new URLSearchParams();
  params.set('page', String(filter.page ?? 1));
  params.set('pageSize', String(filter.pageSize ?? 20));

  if (filter.search?.trim()) params.set('search', filter.search.trim());
  if (filter.isActive !== undefined) params.set('isActive', String(filter.isActive));
  if (filter.isPublished !== undefined) params.set('isPublished', String(filter.isPublished));
  if (filter.isFeatured !== undefined) params.set('isFeatured', String(filter.isFeatured));
  if (filter.sortBy) params.set('sort', `${filter.sortBy}:${filter.desc ? 'desc' : 'asc'}`);

  return `?${params.toString()}`;
}

export const brandsApi = {
  list: (filter: BrandFilter = {}, signal?: AbortSignal) =>
    api.get<PagedResult<BrandListItem>>(`/brands${buildQuery(filter)}`, { signal }),

  lookup: () => api.get<BrandLookupItem[]>('/brands/lookup'),

  get: (id: string) => api.get<BrandDetails>(`/brands/${id}`),

  getBySlug: (slug: string) => api.get<BrandDetails>(`/brands/slug/${encodeURIComponent(slug)}`),

  create: (body: CreateBrandDto) => api.post<CreateBrandResponse>('/brands', body),

  update: (id: string, body: UpdateBrandDto) => api.put<BrandDetails>(`/brands/${id}`, body),

  remove: (id: string) => api.del<void>(`/brands/${id}`),

  restore: (id: string) => api.post<void>(`/brands/${id}/restore`),

  activate: (id: string) => api.post<void>(`/brands/${id}/activate`),
  deactivate: (id: string) => api.post<void>(`/brands/${id}/deactivate`),

  publish: (id: string) => api.post<void>(`/brands/${id}/publish`),
  unpublish: (id: string) => api.post<void>(`/brands/${id}/unpublish`),

  feature: (id: string) => api.post<void>(`/brands/${id}/feature`),
  unfeature: (id: string) => api.post<void>(`/brands/${id}/unfeature`),
};
