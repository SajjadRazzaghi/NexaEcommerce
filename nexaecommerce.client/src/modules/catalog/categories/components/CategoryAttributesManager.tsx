
import { useEffect, useState } from 'react';
import {
    useMutation,
    useQuery,
    useQueryClient,
} from '@tanstack/react-query';
import { Save, RefreshCw } from 'lucide-react';

import { Button } from '@/components/ui/button';
import {
    Card,
    CardContent,
    CardDescription,
    CardHeader,
    CardTitle,
} from '@/components/ui/card';

import {
    catalogAttributesApi,
} from '@/modules/catalog/api/catalogAttributes';

import {
    categoriesApi,
} from '@/modules/catalog/api/categories';

type CategoryAttributesManagerProps = {
    categoryId: string;
};

type AttributeRoleFlags = {
    filterable: boolean;
    descriptive: boolean;
    variantDefining: boolean;
};

type CategoryAttributeConfig = {
    catalogAttributeId: string;
    role: number;
    isRequired: boolean;
    displayOrder: number;
    isActive: boolean;
    name?: string;
    code?: string;
};

type AttributeRow = {
    catalogAttributeId: string;
    name: string;
    code: string;
    isSelected: boolean;
    isRequired: boolean;
    displayOrder: number;
    role: number;
};

const ROLE_FILTERABLE = 1;
const ROLE_DESCRIPTIVE = 2;
const ROLE_VARIANT_DEFINING = 4;

const ALL_ROLE_FLAGS =
    ROLE_FILTERABLE |
    ROLE_DESCRIPTIVE |
    ROLE_VARIANT_DEFINING;

function getDefaultRole(attribute: {
    isFilterable: boolean;
    isVariantAttribute: boolean;
}) {
    let role = 0;

    if (attribute.isFilterable) {
        role |= ROLE_FILTERABLE;
    }

    if (attribute.isVariantAttribute) {
        role |= ROLE_VARIANT_DEFINING;
    } else {
        role |= ROLE_DESCRIPTIVE;
    }

    return role || ROLE_DESCRIPTIVE;
}

function normalizeRole(role: unknown, fallback: number) {
    const parsed = Number(role);

    if (
        !Number.isInteger(parsed) ||
        parsed <= 0 ||
        (parsed & ~ALL_ROLE_FLAGS) !== 0
    ) {
        return fallback;
    }

    return parsed;
}

function getRoleFlags(role: number): AttributeRoleFlags {
    return {
        filterable:
            (role & ROLE_FILTERABLE) !== 0,

        descriptive:
            (role & ROLE_DESCRIPTIVE) !== 0,

        variantDefining:
            (role & ROLE_VARIANT_DEFINING) !== 0,
    };
}

export default function CategoryAttributesManager({
    categoryId,
}: CategoryAttributesManagerProps) {
    const queryClient = useQueryClient();

    const [rows, setRows] = useState<AttributeRow[]>([]);
    const [message, setMessage] = useState<string | null>(null);

    const {
        data: catalogAttributes,
        isLoading: attributesLoading,
        isError: attributesError,
        refetch: refetchAttributes,
    } = useQuery({
        queryKey: ['catalog-attributes'],
        queryFn: () => catalogAttributesApi.list(),
        enabled: Boolean(categoryId),
    });

    const {
        data: categoryConfigurations,
        isLoading: configurationsLoading,
        isError: configurationsError,
        refetch: refetchConfigurations,
    } = useQuery({
        queryKey: [
            'category-attribute-assignments',
            categoryId,
        ],
        queryFn: () =>
            categoriesApi.getAttributeAssignments(categoryId),
        enabled: Boolean(categoryId),
    });

    useEffect(() => {
        if (
            !catalogAttributes ||
            !categoryConfigurations
        ) {
            return;
        }

        const configurations =
            categoryConfigurations as unknown as CategoryAttributeConfig[];

        const configByAttributeId = new Map(
            configurations.map(configuration => [
                configuration.catalogAttributeId,
                configuration,
            ]),
        );

        const nextRows: AttributeRow[] =
            catalogAttributes.map(attribute => {
                const configuration =
                    configByAttributeId.get(attribute.id);

                const defaultRole =
                    getDefaultRole(attribute);

                return {
                    catalogAttributeId: attribute.id,
                    name: attribute.name,
                    code: attribute.code,

                    isSelected:
                        configuration?.isActive ?? false,

                    role: normalizeRole(
                        configuration?.role,
                        defaultRole,
                    ),

                    isRequired:
                        configuration?.isRequired ??
                        attribute.isRequired,

                    displayOrder:
                        configuration?.displayOrder ??
                        attribute.displayOrder,
                };
            });

        nextRows.sort(
            (left, right) =>
                left.displayOrder - right.displayOrder ||
                left.name.localeCompare(right.name),
        );

        setRows(nextRows);
        setMessage(null);
    }, [
        catalogAttributes,
        categoryConfigurations,
    ]);

    const saveMutation = useMutation({
        mutationFn: async () => {
            const selectedRows = rows.filter(
                row => row.isSelected,
            );

            const invalidRole = selectedRows.find(
                row =>
                    row.role === 0 ||
                    (row.role & ~ALL_ROLE_FLAGS) !== 0,
            );

            if (invalidRole) {
                throw new Error(
                    `Select at least one role for "${invalidRole.name}".`,
                );
            }

            const assignments = selectedRows.map(row => ({
                catalogAttributeId: row.catalogAttributeId,
                role: row.role,
                isRequired: row.isRequired,
                displayOrder: row.displayOrder,
                isActive: true,
            }));

            return categoriesApi.replaceAttributeAssignments(
                categoryId,
                assignments,
            );
        },

        onSuccess: async () => {
            setMessage('Category attributes saved successfully.');

            await queryClient.invalidateQueries({
                queryKey: [
                    'category-attribute-assignments',
                    categoryId,
                ],
            });

            await queryClient.invalidateQueries({
                queryKey: ['catalog-attributes'],
            });
        },

        onError: error => {
            setMessage(
                error instanceof Error
                    ? error.message
                    : 'Could not save category attributes.',
            );
        },
    });

    const updateRow = (
        attributeId: string,
        update: Partial<AttributeRow>,
    ) => {
        setRows(current =>
            current.map(row =>
                row.catalogAttributeId === attributeId
                    ? { ...row, ...update }
                    : row,
            ),
        );

        setMessage(null);
    };

    const toggleRole = (
        row: AttributeRow,
        roleFlag: number,
        checked: boolean,
    ) => {
        let nextRole = checked
            ? row.role | roleFlag
            : row.role & ~roleFlag;

        // At least one role must remain selected.
        if (nextRole === 0) {
            setMessage(
                `At least one role must remain selected for "${row.name}".`,
            );
            return;
        }

        nextRole &= ALL_ROLE_FLAGS;

        updateRow(row.catalogAttributeId, {
            role: nextRole,
        });
    };

    const handleRefresh = async () => {
        setMessage(null);

        await Promise.all([
            refetchAttributes(),
            refetchConfigurations(),
        ]);
    };

    const loading =
        attributesLoading || configurationsLoading;

    const failed =
        attributesError || configurationsError;

    return (
        <Card className="md:col-span-2">
            <CardHeader>
                <div className="flex flex-wrap items-start justify-between gap-3">
                    <div>
                        <CardTitle>
                            Category Attributes
                        </CardTitle>

                        <CardDescription>
                            Configure which product attributes are
                            available in this category, and how
                            each attribute is used.
                        </CardDescription>
                    </div>

                    <Button
                        type="button"
                        variant="outline"
                        disabled={loading}
                        onClick={handleRefresh}
                    >
                        <RefreshCw className="mr-2 size-4" />
                        Refresh
                    </Button>
                </div>
            </CardHeader>

            <CardContent className="space-y-4">
                {loading && (
                    <p className="text-muted-foreground text-sm">
                        Loading attributes...
                    </p>
                )}

                {failed && (
                    <div className="space-y-3 rounded-md border border-destructive/40 p-4">
                        <p className="text-sm text-destructive">
                            Could not load category or catalog
                            attributes. Check the API connection
                            and your permissions.
                        </p>

                        <Button
                            type="button"
                            variant="outline"
                            onClick={handleRefresh}
                        >
                            Try again
                        </Button>
                    </div>
                )}

                {!loading &&
                    !failed &&
                    rows.length === 0 && (
                        <p className="text-muted-foreground text-sm">
                            No catalog attributes exist yet.
                            Create attributes before configuring
                            this category.
                        </p>
                    )}

                {!loading &&
                    !failed &&
                    rows.length > 0 && (
                        <div className="space-y-4">
                            {rows.map(row => {
                                const flags =
                                    getRoleFlags(row.role);

                                return (
                                    <div
                                        key={row.catalogAttributeId}
                                        className="space-y-4 rounded-lg border p-4"
                                    >
                                        <div className="flex flex-wrap items-start justify-between gap-4">
                                            <div className="min-w-0">
                                                <p className="font-medium">
                                                    {row.name}
                                                </p>

                                                <p className="text-muted-foreground text-sm">
                                                    {row.code}
                                                </p>
                                            </div>

                                            <label className="flex cursor-pointer items-center gap-2 text-sm">
                                                <input
                                                    type="checkbox"
                                                    checked={row.isSelected}
                                                    onChange={event =>
                                                        updateRow(
                                                            row.catalogAttributeId,
                                                            {
                                                                isSelected:
                                                                    event.target.checked,
                                                            },
                                                        )
                                                    }
                                                />

                                                Available in category
                                            </label>
                                        </div>

                                        <fieldset
                                            disabled={!row.isSelected}
                                            className="space-y-3"
                                        >
                                            <legend className="mb-2 text-sm font-medium">
                                                Attribute roles
                                            </legend>

                                            <div className="flex flex-wrap gap-x-6 gap-y-3">
                                                <label className="flex cursor-pointer items-center gap-2 text-sm">
                                                    <input
                                                        type="checkbox"
                                                        checked={flags.descriptive}
                                                        onChange={event =>
                                                            toggleRole(
                                                                row,
                                                                ROLE_DESCRIPTIVE,
                                                                event.target.checked,
                                                            )
                                                        }
                                                    />

                                                    Descriptive
                                                </label>

                                                <label className="flex cursor-pointer items-center gap-2 text-sm">
                                                    <input
                                                        type="checkbox"
                                                        checked={flags.filterable}
                                                        onChange={event =>
                                                            toggleRole(
                                                                row,
                                                                ROLE_FILTERABLE,
                                                                event.target.checked,
                                                            )
                                                        }
                                                    />

                                                    Filterable
                                                </label>

                                                <label className="flex cursor-pointer items-center gap-2 text-sm">
                                                    <input
                                                        type="checkbox"
                                                        checked={flags.variantDefining}
                                                        onChange={event =>
                                                            toggleRole(
                                                                row,
                                                                ROLE_VARIANT_DEFINING,
                                                                event.target.checked,
                                                            )
                                                        }
                                                    />

                                                    Variant defining
                                                </label>
                                            </div>

                                            <div className="flex flex-wrap items-center gap-5 border-t pt-3">
                                                <label className="flex cursor-pointer items-center gap-2 text-sm">
                                                    <input
                                                        type="checkbox"
                                                        checked={row.isRequired}
                                                        onChange={event =>
                                                            updateRow(
                                                                row.catalogAttributeId,
                                                                {
                                                                    isRequired:
                                                                        event.target.checked,
                                                                },
                                                            )
                                                        }
                                                    />

                                                    Required
                                                </label>

                                                <label className="flex items-center gap-2 text-sm">
                                                    Display order

                                                    <input
                                                        type="number"
                                                        min={0}
                                                        className="border-input bg-background h-9 w-24 rounded-md border px-2"
                                                        value={row.displayOrder}
                                                        onChange={event => {
                                                            const parsed =
                                                                Number(event.target.value);

                                                            updateRow(
                                                                row.catalogAttributeId,
                                                                {
                                                                    displayOrder:
                                                                        Number.isFinite(parsed)
                                                                            ? Math.max(
                                                                                0,
                                                                                Math.trunc(parsed),
                                                                            )
                                                                            : 0,
                                                                },
                                                            );
                                                        }}
                                                    />
                                                </label>
                                            </div>
                                        </fieldset>
                                    </div>
                                );
                            })}

                            <div className="flex flex-wrap items-center justify-between gap-3 border-t pt-4">
                                <p className="text-muted-foreground text-sm">
                                    {rows.filter(row => row.isSelected).length}
                                    {' '}attribute(s) selected
                                </p>

                                <Button
                                    type="button"
                                    disabled={
                                        loading ||
                                        saveMutation.isPending
                                    }
                                    onClick={() =>
                                        saveMutation.mutate()
                                    }
                                >
                                    <Save className="mr-2 size-4" />

                                    {saveMutation.isPending
                                        ? 'Saving...'
                                        : 'Save category attributes'}
                                </Button>
                            </div>
                        </div>
                    )}

                {message && (
                    <p
                        role="status"
                        className="text-sm"
                    >
                        {message}
                    </p>
                )}
            </CardContent>
        </Card>
    );
}
