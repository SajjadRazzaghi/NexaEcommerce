import { api } from '@/lib/api/client';

export type CatalogAttributeValue = {
    id: string;
    catalogAttributeId: string;

    value: string;
    displayValue?: string | null;
    colorHex?: string | null;

    displayOrder: number;
    isActive: boolean;
};

export type CatalogAttribute = {
    id: string;

    name: string;
    code: string;

    description?: string | null;
    displayType?: string | null;

    isRequired: boolean;
    isFilterable: boolean;
    isVariantAttribute: boolean;
    isActive: boolean;

    displayOrder: number;

    values: CatalogAttributeValue[];
};

export type CreateCatalogAttributeDto = {
    name: string;
    code: string;
    description?: string | null;
    displayType?: string | null;

    isRequired: boolean;
    isFilterable: boolean;
    isVariantAttribute: boolean;
    isActive: boolean;

    displayOrder: number;
};

export type UpdateCatalogAttributeDto =
    CreateCatalogAttributeDto;

export type CreateCatalogAttributeValueDto = {
    value: string;
    displayValue?: string | null;
    colorHex?: string | null;

    displayOrder: number;
    isActive: boolean;
};

export type UpdateCatalogAttributeValueDto =
    CreateCatalogAttributeValueDto;

export const catalogAttributesApi = {
    list: (
        search?: string,
        signal?: AbortSignal,
    ) =>
        api.get<CatalogAttribute[]>(
            '/catalog/attributes',
            {
                params: search?.trim()
                    ? {
                        search: search.trim(),
                    }
                    : undefined,
                signal,
            },
        ),

    get: (id: string) =>
        api.get<CatalogAttribute>(
            `/catalog/attributes/${id}`,
        ),

    create: (
        data: CreateCatalogAttributeDto,
    ) =>
        api.post<CatalogAttribute>(
            '/catalog/attributes',
            data,
        ),

    update: (
        id: string,
        data: UpdateCatalogAttributeDto,
    ) =>
        api.put<CatalogAttribute>(
            `/catalog/attributes/${id}`,
            data,
        ),

    remove: (id: string) =>
        api.del<void>(
            `/catalog/attributes/${id}`,
        ),

    addValue: (
        attributeId: string,
        data: CreateCatalogAttributeValueDto,
    ) =>
        api.post<CatalogAttributeValue>(
            `/catalog/attributes/${attributeId}/values`,
            data,
        ),

    updateValue: (
        attributeId: string,
        valueId: string,
        data: UpdateCatalogAttributeValueDto,
    ) =>
        api.put<CatalogAttributeValue>(
            `/catalog/attributes/${attributeId}/values/${valueId}`,
            data,
        ),

    removeValue: (
        attributeId: string,
        valueId: string,
    ) =>
        api.del<void>(
            `/catalog/attributes/${attributeId}/values/${valueId}`,
        ),
};