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

export const catalogAttributesApi = {
    getAll: (search?: string) =>
        api.get<CatalogAttribute[]>(
            '/catalog/attributes',
            {
                params: search?.trim()
                    ? { search: search.trim() }
                    : undefined,
            },
        ),

    getById: (id: string) =>
        api.get<CatalogAttribute>(
            `/ catalog / attributes / ${ id } `,
        ),
};

