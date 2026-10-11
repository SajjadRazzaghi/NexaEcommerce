
import { api } from '@/lib/api/client';


export enum CatalogAttributeDataType {
    Text = 1,
    Integer = 2,
    Decimal = 3,
    Boolean = 4,
    Date = 5,
    DateTime = 6,
    SingleSelect = 7,
    MultiSelect = 8,
}
export type CatalogAttributeValue = {
    id: string;
    catalogAttributeId: string;
    value: string;
    displayValue?: string | null;
    colorHex?: string | null;
    displayOrder: number;
    isActive: boolean;
};

export type CatalogAttributeDisplayType = {
    id: string;
    code: string;
    nameEn: string;
    nameFa: string;
    displayOrder: number;
};

export type CatalogAttribute = {
    id: string;
    name: string;
    code: string;
    description?: string | null;
    displayType?: string | null;
    dataType: CatalogAttributeDataType;

    // Used when an attribute is returned as a product definition.
    defaultRole?: number;

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
    dataType: CatalogAttributeDataType;
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

export type ProductAttributeDefinitionValue = {
    catalogAttributeValueId: string;
    value: string;
    displayValue?: string | null;
    colorHex?: string | null;
    displayOrder: number;
    isActive: boolean;
};

export type ProductAttributeDefinition = {
    catalogAttributeId: string;
    name: string;
    code: string;
    dataType: CatalogAttributeDataType;
    defaultRole: number;
    isRequired: boolean;
    isFilterable: boolean;
    displayType?: string | null;
    displayOrder: number;
    values: ProductAttributeDefinitionValue[];
};

export const catalogAttributesApi = {
    list: (
        search?: string,
        signal?: AbortSignal,
    ) =>
        api.get<CatalogAttribute[]>(
            '/catalog/attributes',
            {
                params: search?.trim()
                    ? { search: search.trim() }
                    : undefined,
                signal,
            },
        ),

    listDisplayTypes: (
        signal?: AbortSignal,
    ) =>
        api.get<CatalogAttributeDisplayType[]>(
            '/catalog/attribute-display-types',
            { signal },
        ),

    listProductDefinitions: (
        categoryIds: string[],
        productId?: string,
        signal?: AbortSignal,
    ) =>
        api.get<ProductAttributeDefinition[]>(
            '/catalog/attribute-definitions',
            {
                params: {
                    categoryIds: categoryIds.join(','),
                    productId: productId || undefined,
                },
                signal,
            },
        ),

    get: (id: string) =>
        api.get<CatalogAttribute>(
            `/catalog/attributes/${id}`,
        ),

    create: (data: CreateCatalogAttributeDto) =>
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
