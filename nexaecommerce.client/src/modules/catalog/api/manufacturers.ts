import { api } from '@/lib/api/client';
import type { PagedResult } from '@/lib/api/paged';

export interface ManufacturerListItem {
  id: string;
  name: string;
  slug: string;
  logoUrl?: string | null;
  displayOrder: number;
  isActive: boolean;
  isPublished: boolean;
  isFeatured: boolean;
  productCount: number;
}

export interface ManufacturerDetails extends ManufacturerListItem {
  description?: string | null;
  website?: string | null;
  coverImageUrl?: string | null;
  seoTitle?: string | null;
  seoDescription?: string | null;
  seoKeywords?: string | null;
  createdAt?: string;
  updatedAt?: string | null;
}

export interface ManufacturerLookupItem {
  id: string;
  name: string;
  slug: string;
}

export interface ManufacturerFilter {
  page?: number;
  pageSize?: number;
  search?: string;
  isActive?: boolean;
  isPublished?: boolean;
  isFeatured?: boolean;
  sortBy?: string;
  desc?: boolean;
}

export interface CreateManufacturerDto {
  name: string;
  description?: string | null;
  website?: string | null;
  logoUrl?: string | null;
  coverImageUrl?: string | null;
  seoTitle?: string | null;
  seoDescription?: string | null;
  seoKeywords?: string | null;
}

export interface UpdateManufacturerDto extends CreateManufacturerDto {
  slug?: string | null;
  isActive: boolean;
  isPublished: boolean;
  isFeatured: boolean;
  displayOrder: number;
}

export interface CreateManufacturerResponse {
  id: string;
}

function buildQuery(filter: ManufacturerFilter = {}) {
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

export const manufacturersApi = {
  list: (filter: ManufacturerFilter = {}, signal?: AbortSignal) =>
    api.get<PagedResult<ManufacturerListItem>>(`/manufacturers${buildQuery(filter)}`, { signal }),

  lookup: () => api.get<ManufacturerLookupItem[]>('/manufacturers/lookup'),

  get: (id: string) => api.get<ManufacturerDetails>(`/manufacturers/${id}`),

  getBySlug: (slug: string) =>
    api.get<ManufacturerDetails>(`/manufacturers/slug/${encodeURIComponent(slug)}`),

  create: (body: CreateManufacturerDto) =>
    api.post<CreateManufacturerResponse>('/manufacturers', body),

  update: (id: string, body: UpdateManufacturerDto) =>
    api.put<ManufacturerDetails>(`/manufacturers/${id}`, body),

  remove: (id: string) => api.del<void>(`/manufacturers/${id}`),
  restore: (id: string) => api.post<void>(`/manufacturers/${id}/restore`),
  activate: (id: string) => api.post<void>(`/manufacturers/${id}/activate`),
  deactivate: (id: string) => api.post<void>(`/manufacturers/${id}/deactivate`),
  publish: (id: string) => api.post<void>(`/manufacturers/${id}/publish`),
  unpublish: (id: string) => api.post<void>(`/manufacturers/${id}/unpublish`),
  feature: (id: string) => api.post<void>(`/manufacturers/${id}/feature`),
  unfeature: (id: string) => api.post<void>(`/manufacturers/${id}/unfeature`),
};
