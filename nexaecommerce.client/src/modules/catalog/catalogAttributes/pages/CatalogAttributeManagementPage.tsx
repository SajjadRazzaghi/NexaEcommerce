import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import {
    Plus,
    Pencil,
    Trash2,
    ChevronDown,
    ChevronRight,
    Power,
    PowerOff,
} from 'lucide-react';

import {
    Card,
    CardContent,
    CardDescription,
    CardHeader,
    CardTitle,
} from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Textarea } from '@/components/ui/textarea';

import type {
    CatalogAttribute,
    CreateCatalogAttributeDto,
    CreateCatalogAttributeValueDto,
} from '@/modules/catalog/api/catalogAttributes';
import {
    useCatalogAttributes,
    useCreateCatalogAttribute,
    useUpdateCatalogAttribute,
    useDeleteCatalogAttribute,
    useCatalogAttributeValueMutations,
} from '@/modules/catalog/catalogAttributes/hooks';

type AttributeFormState = {
    name: string;
    code: string;
    description: string;
    displayType: string;
    isRequired: boolean;
    isFilterable: boolean;
    isVariantAttribute: boolean;
    isActive: boolean;
    displayOrder: number;
};

type ValueFormState = {
    value: string;
    displayValue: string;
    colorHex: string;
    displayOrder: number;
    isActive: boolean;
};

const emptyAttributeForm: AttributeFormState = {
    name: '',
    code: '',
    description: '',
    displayType: 'select',
    isRequired: false,
    isFilterable: false,
    isVariantAttribute: true,
    isActive: true,
    displayOrder: 0,
};

const emptyValueForm: ValueFormState = {
    value: '',
    displayValue: '',
    colorHex: '',
    displayOrder: 0,
    isActive: true,
};

function normalizeCode(value: string) {
    return value
        .trim()
        .toLowerCase()
        .replace(/\s+/g, '-');
}

export default function CatalogAttributeManagementPage() {
    const { t } = useTranslation();

    const [search, setSearch] = useState('');
    const [expandedId, setExpandedId] =
        useState<string | null>(null);

    const [editingId, setEditingId] =
        useState<string | null>(null);

    const [attributeForm, setAttributeForm] =
        useState<AttributeFormState>(
            emptyAttributeForm,
        );

    const [addingValueFor, setAddingValueFor] =
        useState<string | null>(null);

    const [editingValueId, setEditingValueId] =
        useState<string | null>(null);

    const [valueForm, setValueForm] =
        useState<ValueFormState>(
            emptyValueForm,
        );

    const {
        data: attributes = [],
        isLoading,
        isError,
        error,
    } = useCatalogAttributes(search);

    const createAttribute =
        useCreateCatalogAttribute();

    const updateAttribute =
        useUpdateCatalogAttribute();

    const deleteAttribute =
        useDeleteCatalogAttribute();

    const {
        add: addValue,
        update: updateValue,
        remove: removeValue,
    } = useCatalogAttributeValueMutations();

    const sortedAttributes = useMemo(
        () =>
            [...attributes].sort(
                (a, b) =>
                    a.displayOrder - b.displayOrder ||
                    a.name.localeCompare(
                        b.name,
                    ),
            ),
        [attributes],
    );

    const startCreateAttribute = () => {
        setEditingId(null);

        setAttributeForm({
            ...emptyAttributeForm,
            displayOrder:
                attributes.length,
        });
    };

    const startEditAttribute = (
        attribute: CatalogAttribute,
    ) => {
        setEditingId(attribute.id);

        setAttributeForm({
            name: attribute.name,
            code: attribute.code,
            description:
                attribute.description ?? '',
            displayType:
                attribute.displayType ??
                'select',
            isRequired:
                attribute.isRequired,
            isFilterable:
                attribute.isFilterable,
            isVariantAttribute:
                attribute.isVariantAttribute,
            isActive:
                attribute.isActive,
            displayOrder:
                attribute.displayOrder,
        });
    };

    const cancelAttributeEdit = () => {
        setEditingId(null);

        setAttributeForm(
            emptyAttributeForm,
        );
    };

    const submitAttribute = async () => {
        const body: CreateCatalogAttributeDto = {
            name:
                attributeForm.name.trim(),

            code: normalizeCode(
                attributeForm.code ||
                    attributeForm.name,
            ),

            description:
                attributeForm.description.trim() ||
                null,

            displayType:
                attributeForm.displayType.trim() ||
                null,

            isRequired:
                attributeForm.isRequired,

            isFilterable:
                attributeForm.isFilterable,

            isVariantAttribute:
                attributeForm.isVariantAttribute,

            isActive:
                attributeForm.isActive,

            displayOrder: Math.max(
                0,
                attributeForm.displayOrder,
            ),
        };

        if (!body.name) {
            return;
        }

        if (editingId) {
            await updateAttribute.mutateAsync({
                id: editingId,
                body,
            });
        } else {
            await createAttribute.mutateAsync(
                body,
            );
        }

        cancelAttributeEdit();
    };

    const handleDeleteAttribute = async (
        attribute: CatalogAttribute,
    ) => {
        const confirmed = window.confirm(
            t(
                'catalogAttributes.confirm.deleteAttribute',
                {
                    name: attribute.name,
                },
            ),
        );

        if (!confirmed) {
            return;
        }

        await deleteAttribute.mutateAsync(
            attribute.id,
        );

        if (expandedId === attribute.id) {
            setExpandedId(null);
        }
    };

    const startAddValue = (
        attribute: CatalogAttribute,
    ) => {
        setExpandedId(attribute.id);
        setAddingValueFor(attribute.id);
        setEditingValueId(null);

        setValueForm({
            ...emptyValueForm,
            displayOrder:
                attribute.values.length,
        });
    };

    const startEditValue = (
        value: CatalogAttribute['values'][number],
    ) => {
        setAddingValueFor(
            value.catalogAttributeId,
        );

        setEditingValueId(value.id);

        setValueForm({
            value: value.value,
            displayValue:
                value.displayValue ?? '',
            colorHex:
                value.colorHex ?? '',
            displayOrder:
                value.displayOrder,
            isActive:
                value.isActive,
        });
    };

    const cancelValueEdit = () => {
        setAddingValueFor(null);
        setEditingValueId(null);

        setValueForm(
            emptyValueForm,
        );
    };

    const submitValue = async (
        attributeId: string,
    ) => {
        const body: CreateCatalogAttributeValueDto =
            {
                value:
                    valueForm.value.trim(),

                displayValue:
                    valueForm.displayValue.trim() ||
                    null,

                colorHex:
                    valueForm.colorHex.trim() ||
                    null,

                displayOrder: Math.max(
                    0,
                    valueForm.displayOrder,
                ),

                isActive:
                    valueForm.isActive,
            };

        if (!body.value) {
            return;
        }

        if (editingValueId) {
            await updateValue.mutateAsync({
                attributeId,
                valueId:
                    editingValueId,
                body,
            });
        } else {
            await addValue.mutateAsync({
                attributeId,
                body,
            });
        }

        cancelValueEdit();
    };

    const handleDeleteValue = async (
        attributeId: string,
        valueId: string,
        valueLabel: string,
    ) => {
        const confirmed = window.confirm(
            t(
                'catalogAttributes.confirm.deleteValue',
                {
                    name: valueLabel,
                },
            ),
        );

        if (!confirmed) {
            return;
        }

        await removeValue.mutateAsync({
            attributeId,
            valueId,
        });
    };

    if (isLoading) {
        return (
            <div className="space-y-6">
                <div>
                    <h1 className="text-2xl font-bold">
                        {t(
                            'catalogAttributes.title',
                        )}
                    </h1>

                    <p className="text-muted-foreground">
                        {t(
                            'catalogAttributes.loading',
                        )}
                    </p>
                </div>
            </div>
        );
    }

    if (isError) {
        return (
            <Card>
                <CardHeader>
                    <CardTitle>
                        {t(
                            'catalogAttributes.loadError',
                        )}
                    </CardTitle>

                    <CardDescription>
                        {error instanceof Error
                            ? error.message
                            : t(
                                  'catalogAttributes.unexpectedError',
                              )}
                    </CardDescription>
                </CardHeader>
            </Card>
        );
    }

    return (
        <div className="space-y-6">
            {/* Page Header */}
            <div className="flex flex-col gap-4 md:flex-row md:items-center md:justify-between">
                <div>
                    <h1 className="text-2xl font-bold">
                        {t(
                            'catalogAttributes.title',
                        )}
                    </h1>

                    <p className="text-muted-foreground">
                        {t(
                            'catalogAttributes.description',
                        )}
                    </p>
                </div>

                <Button
                    type="button"
                    onClick={
                        startCreateAttribute
                    }
                >
                    <Plus className="me-2 size-4" />

                    {t(
                        'catalogAttributes.newAttribute',
                    )}
                </Button>
            </div>

            {/* Search */}
            <Card>
                <CardContent className="pt-6">
                    <Input
                        value={search}
                        onChange={(event) =>
                            setSearch(
                                event.target.value,
                            )
                        }
                        placeholder={t(
                            'catalogAttributes.searchPlaceholder',
                        )}
                    />
                </CardContent>
            </Card>

            {/* Saving State */}
            {(createAttribute.isPending ||
                updateAttribute.isPending ||
                deleteAttribute.isPending) && (
                <div className="text-muted-foreground text-sm">
                    {t(
                        'catalogAttributes.saving',
                    )}
                </div>
            )}

            {editingId === null && (
                <Card>
                        <CardHeader>
                            <CardTitle>
                                {t(
                                    'catalogAttributes.form.title',
                                )}
                            </CardTitle>

                            <CardDescription>
                                {t(
                                    'catalogAttributes.form.description',
                                )}
                            </CardDescription>
                        </CardHeader>

                        <CardContent>
                            <div className="grid gap-4 md:grid-cols-2">
                                <Input
                                    value={
                                        attributeForm.name
                                    }
                                    onChange={(
                                        event,
                                    ) =>
                                        setAttributeForm(
                                            (current) => ({
                                                ...current,
                                                name: event
                                                    .target
                                                    .value,
                                            }),
                                        )
                                    }
                                    placeholder={t(
                                        'catalogAttributes.form.name',
                                    )}
                                />

                                <Input
                                    value={
                                        attributeForm.code
                                    }
                                    onChange={(
                                        event,
                                    ) =>
                                        setAttributeForm(
                                            (current) => ({
                                                ...current,
                                                code: event
                                                    .target
                                                    .value,
                                            }),
                                        )
                                    }
                                    placeholder={t(
                                        'catalogAttributes.form.code',
                                    )}
                                />

                            <select
                                value={attributeForm.displayType}
                                onChange={(event) =>
                                    setAttributeForm((current) => ({
                                        ...current,
                                        displayType: event.target.value,
                                    }))
                                }
                                className="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background focus:outline-none focus:ring-2 focus:ring-ring focus:ring-offset-2"
                            >
                                <option value="select">
                                    {t(
                                        'catalogAttributes.form.displayTypeOptions.select',
                                    )}
                                </option>

                                <option value="color">
                                    {t(
                                        'catalogAttributes.form.displayTypeOptions.color',
                                    )}
                                </option>
                            </select>

                                <Input
                                    type="number"
                                    min={0}
                                    value={
                                        attributeForm.displayOrder
                                    }
                                    onChange={(
                                        event,
                                    ) =>
                                        setAttributeForm(
                                            (current) => ({
                                                ...current,
                                                displayOrder:
                                                    Number(
                                                        event
                                                            .target
                                                            .value,
                                                    ),
                                            }),
                                        )
                                    }
                                    placeholder={t(
                                        'catalogAttributes.form.displayOrder',
                                    )}
                                />

                                <div className="md:col-span-2">
                                    <Textarea
                                        value={
                                            attributeForm.description
                                        }
                                        onChange={(
                                            event,
                                        ) =>
                                            setAttributeForm(
                                                (
                                                    current,
                                                ) => ({
                                                    ...current,
                                                    description:
                                                        event
                                                            .target
                                                            .value,
                                                }),
                                            )
                                        }
                                        placeholder={t(
                                            'catalogAttributes.form.descriptionField',
                                        )}
                                    />
                                </div>

                                <label className="flex items-center gap-2 text-sm">
                                    <input
                                        type="checkbox"
                                        checked={
                                            attributeForm.isRequired
                                        }
                                        onChange={(
                                            event,
                                        ) =>
                                            setAttributeForm(
                                                (
                                                    current,
                                                ) => ({
                                                    ...current,
                                                    isRequired:
                                                        event
                                                            .target
                                                            .checked,
                                                }),
                                            )
                                        }
                                    />

                                    {t(
                                        'catalogAttributes.form.required',
                                    )}
                                </label>

                                <label className="flex items-center gap-2 text-sm">
                                    <input
                                        type="checkbox"
                                        checked={
                                            attributeForm.isFilterable
                                        }
                                        onChange={(
                                            event,
                                        ) =>
                                            setAttributeForm(
                                                (
                                                    current,
                                                ) => ({
                                                    ...current,
                                                    isFilterable:
                                                        event
                                                            .target
                                                            .checked,
                                                }),
                                            )
                                        }
                                    />

                                    {t(
                                        'catalogAttributes.form.filterable',
                                    )}
                                </label>

                                <label className="flex items-center gap-2 text-sm">
                                    <input
                                        type="checkbox"
                                        checked={
                                            attributeForm.isVariantAttribute
                                        }
                                        onChange={(
                                            event,
                                        ) =>
                                            setAttributeForm(
                                                (
                                                    current,
                                                ) => ({
                                                    ...current,
                                                    isVariantAttribute:
                                                        event
                                                            .target
                                                            .checked,
                                                }),
                                            )
                                        }
                                    />

                                    {t(
                                        'catalogAttributes.form.variantAttribute',
                                    )}
                                </label>

                                <label className="flex items-center gap-2 text-sm">
                                    <input
                                        type="checkbox"
                                        checked={
                                            attributeForm.isActive
                                        }
                                        onChange={(
                                            event,
                                        ) =>
                                            setAttributeForm(
                                                (
                                                    current,
                                                ) => ({
                                                    ...current,
                                                    isActive:
                                                        event
                                                            .target
                                                            .checked,
                                                }),
                                            )
                                        }
                                    />

                                    {t(
                                        'catalogAttributes.form.active',
                                    )}
                                </label>

                                <div className="flex gap-2 md:col-span-2">
                                    <Button
                                        type="button"
                                        onClick={
                                            submitAttribute
                                        }
                                        disabled={
                                            createAttribute.isPending
                                        }
                                    >
                                        {t(
                                            'catalogAttributes.form.create',
                                        )}
                                    </Button>

                                    <Button
                                        type="button"
                                        variant="ghost"
                                        onClick={
                                            cancelAttributeEdit
                                        }
                                    >
                                        {t(
                                            'catalogAttributes.form.reset',
                                        )}
                                    </Button>
                                </div>
                            </div>
                        </CardContent>
                    </Card>
                )}

            {/* Edit Attribute Form */}
            {editingId !== null && (
                <Card>
                    <CardHeader>
                        <CardTitle>
                            {t(
                                'catalogAttributes.form.editTitle',
                            )}
                        </CardTitle>
                    </CardHeader>

                    <CardContent>
                        <div className="grid gap-4 md:grid-cols-2">
                            <Input
                                value={
                                    attributeForm.name
                                }
                                onChange={(
                                    event,
                                ) =>
                                    setAttributeForm(
                                        (current) => ({
                                            ...current,
                                            name: event
                                                .target
                                                .value,
                                        }),
                                    )
                                }
                                placeholder={t(
                                    'catalogAttributes.form.name',
                                )}
                            />

                            <Input
                                value={
                                    attributeForm.code
                                }
                                onChange={(
                                    event,
                                ) =>
                                    setAttributeForm(
                                        (current) => ({
                                            ...current,
                                            code: event
                                                .target
                                                .value,
                                        }),
                                    )
                                }
                                placeholder={t(
                                    'catalogAttributes.form.code',
                                )}
                            />

                            <select
                                value={attributeForm.displayType}
                                onChange={(event) =>
                                    setAttributeForm((current) => ({
                                        ...current,
                                        displayType: event.target.value,
                                    }))
                                }
                                className="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background focus:outline-none focus:ring-2 focus:ring-ring focus:ring-offset-2"
                            >
                                <option value="select">
                                    {t(
                                        'catalogAttributes.form.displayTypeOptions.select',
                                    )}
                                </option>

                                <option value="color">
                                    {t(
                                        'catalogAttributes.form.displayTypeOptions.color',
                                    )}
                                </option>
                            </select>

                            <Input
                                type="number"
                                min={0}
                                value={
                                    attributeForm.displayOrder
                                }
                                onChange={(
                                    event,
                                ) =>
                                    setAttributeForm(
                                        (current) => ({
                                            ...current,
                                            displayOrder:
                                                Number(
                                                    event
                                                        .target
                                                        .value,
                                                ),
                                        }),
                                    )
                                }
                                placeholder={t(
                                    'catalogAttributes.form.displayOrder',
                                )}
                            />

                            <Textarea
                                value={
                                    attributeForm.description
                                }
                                onChange={(
                                    event,
                                ) =>
                                    setAttributeForm(
                                        (current) => ({
                                            ...current,
                                            description:
                                                event
                                                    .target
                                                    .value,
                                        }),
                                    )
                                }
                                placeholder={t(
                                    'catalogAttributes.form.descriptionField',
                                )}
                            />

                            <div className="space-y-2 text-sm">
                                <label className="flex items-center gap-2">
                                    <input
                                        type="checkbox"
                                        checked={
                                            attributeForm.isRequired
                                        }
                                        onChange={(
                                            event,
                                        ) =>
                                            setAttributeForm(
                                                (
                                                    current,
                                                ) => ({
                                                    ...current,
                                                    isRequired:
                                                        event
                                                            .target
                                                            .checked,
                                                }),
                                            )
                                        }
                                    />

                                    {t(
                                        'catalogAttributes.form.required',
                                    )}
                                </label>

                                <label className="flex items-center gap-2">
                                    <input
                                        type="checkbox"
                                        checked={
                                            attributeForm.isFilterable
                                        }
                                        onChange={(
                                            event,
                                        ) =>
                                            setAttributeForm(
                                                (
                                                    current,
                                                ) => ({
                                                    ...current,
                                                    isFilterable:
                                                        event
                                                            .target
                                                            .checked,
                                                }),
                                            )
                                        }
                                    />

                                    {t(
                                        'catalogAttributes.form.filterable',
                                    )}
                                </label>

                                <label className="flex items-center gap-2">
                                    <input
                                        type="checkbox"
                                        checked={
                                            attributeForm.isVariantAttribute
                                        }
                                        onChange={(
                                            event,
                                        ) =>
                                            setAttributeForm(
                                                (
                                                    current,
                                                ) => ({
                                                    ...current,
                                                    isVariantAttribute:
                                                        event
                                                            .target
                                                            .checked,
                                                }),
                                            )
                                        }
                                    />

                                    {t(
                                        'catalogAttributes.form.variantAttribute',
                                    )}
                                </label>

                                <label className="flex items-center gap-2">
                                    <input
                                        type="checkbox"
                                        checked={
                                            attributeForm.isActive
                                        }
                                        onChange={(
                                            event,
                                        ) =>
                                            setAttributeForm(
                                                (
                                                    current,
                                                ) => ({
                                                    ...current,
                                                    isActive:
                                                        event
                                                            .target
                                                            .checked,
                                                }),
                                            )
                                        }
                                    />

                                    {t(
                                        'catalogAttributes.form.active',
                                    )}
                                </label>
                            </div>

                            <div className="flex gap-2 md:col-span-2">
                                <Button
                                    type="button"
                                    onClick={
                                        submitAttribute
                                    }
                                    disabled={
                                        updateAttribute.isPending
                                    }
                                >
                                    {t(
                                        'catalogAttributes.form.update',
                                    )}
                                </Button>

                                <Button
                                    type="button"
                                    variant="ghost"
                                    onClick={
                                        cancelAttributeEdit
                                    }
                                >
                                    {t(
                                        'catalogAttributes.form.cancel',
                                    )}
                                </Button>
                            </div>
                        </div>
                    </CardContent>
                </Card>
            )}

            {/* Attributes List */}
            <div className="space-y-4">
                {sortedAttributes.map(
                    (attribute) => {
                        const expanded =
                            expandedId ===
                            attribute.id;

                        const adding =
                            addingValueFor ===
                            attribute.id;

                        return (
                            <Card
                                key={
                                    attribute.id
                                }
                            >
                                <CardHeader>
                                    <div className="flex flex-col gap-4 lg:flex-row lg:items-center lg:justify-between">
                                        <div className="flex items-start gap-2">
                                            <Button
                                                type="button"
                                                variant="ghost"
                                                size="sm"
                                                onClick={() =>
                                                    setExpandedId(
                                                        expanded
                                                            ? null
                                                            : attribute.id,
                                                    )
                                                }
                                            >
                                                {expanded ? (
                                                    <ChevronDown className="size-4" />
                                                ) : (
                                                    <ChevronRight className="size-4" />
                                                )}
                                            </Button>

                                            <div>
                                                <CardTitle>
                                                    {
                                                        attribute.name
                                                    }
                                                </CardTitle>

                                                <CardDescription>
                                                    <span className="me-2">
                                                        {
                                                            attribute.code
                                                        }
                                                    </span>

                                                    {attribute.isVariantAttribute
                                                        ? t(
                                                              'catalogAttributes.list.variant',
                                                          )
                                                        : t(
                                                              'catalogAttributes.list.product',
                                                          )}{' '}
                                                    {t(
                                                        'catalogAttributes.list.attribute',
                                                    )}
                                                </CardDescription>
                                            </div>
                                        </div>

                                        <div className="flex flex-wrap gap-2">
                                            <Button
                                                type="button"
                                                variant="outline"
                                                size="sm"
                                                onClick={() =>
                                                    startEditAttribute(
                                                        attribute,
                                                    )
                                                }
                                            >
                                                <Pencil className="me-1 size-4" />

                                                {t(
                                                    'catalogAttributes.list.edit',
                                                )}
                                            </Button>

                                            <Button
                                                type="button"
                                                variant="outline"
                                                size="sm"
                                                onClick={() =>
                                                    startAddValue(
                                                        attribute,
                                                    )
                                                }
                                            >
                                                <Plus className="me-1 size-4" />

                                                {t(
                                                    'catalogAttributes.list.addValue',
                                                )}
                                            </Button>

                                            <Button
                                                type="button"
                                                variant="destructive"
                                                size="sm"
                                                onClick={() =>
                                                    handleDeleteAttribute(
                                                        attribute,
                                                    )
                                                }
                                            >
                                                <Trash2 className="me-1 size-4" />

                                                {t(
                                                    'catalogAttributes.list.delete',
                                                )}
                                            </Button>
                                        </div>
                                    </div>
                                </CardHeader>

                                {expanded && (
                                    <CardContent className="space-y-4">
                                        <div className="grid gap-3 md:grid-cols-4">
                                            <div>
                                                <div className="text-muted-foreground text-xs">
                                                    {t(
                                                        'catalogAttributes.list.values',
                                                    )}
                                                </div>

                                                <div className="font-semibold">
                                                    {
                                                        attribute
                                                            .values
                                                            .length
                                                    }
                                                </div>
                                            </div>

                                            <div>
                                                <div className="text-muted-foreground text-xs">
                                                    {t(
                                                        'catalogAttributes.list.required',
                                                    )}
                                                </div>

                                                <div className="font-semibold">
                                                    {attribute.isRequired
                                                        ? t(
                                                              'catalogAttributes.list.yes',
                                                          )
                                                        : t(
                                                              'catalogAttributes.list.no',
                                                          )}
                                                </div>
                                            </div>

                                            <div>
                                                <div className="text-muted-foreground text-xs">
                                                    {t(
                                                        'catalogAttributes.list.filterable',
                                                    )}
                                                </div>

                                                <div className="font-semibold">
                                                    {attribute.isFilterable
                                                        ? t(
                                                              'catalogAttributes.list.yes',
                                                          )
                                                        : t(
                                                              'catalogAttributes.list.no',
                                                          )}
                                                </div>
                                            </div>

                                            <div>
                                                <div className="text-muted-foreground text-xs">
                                                    {t(
                                                        'catalogAttributes.list.status',
                                                    )}
                                                </div>

                                                <div className="font-semibold">
                                                    {attribute.isActive
                                                        ? t(
                                                              'catalogAttributes.list.active',
                                                          )
                                                        : t(
                                                              'catalogAttributes.list.inactive',
                                                          )}
                                                </div>
                                            </div>
                                        </div>

                                        {/* Add/Edit Value */}
                                        {adding && (
                                            <div className="rounded-lg border p-4">
                                                <div className="mb-3 font-semibold">
                                                    {editingValueId
                                                        ? t(
                                                              'catalogAttributes.values.edit',
                                                          )
                                                        : t(
                                                              'catalogAttributes.values.add',
                                                          )}
                                                </div>

                                                <div className="grid gap-3 md:grid-cols-2">
                                                    <Input
                                                        value={
                                                            valueForm.value
                                                        }
                                                        onChange={(
                                                            event,
                                                        ) =>
                                                            setValueForm(
                                                                (
                                                                    current,
                                                                ) => ({
                                                                    ...current,
                                                                    value: event
                                                                        .target
                                                                        .value,
                                                                }),
                                                            )
                                                        }
                                                        placeholder={t(
                                                            'catalogAttributes.values.value',
                                                        )}
                                                    />

                                                    <Input
                                                        value={
                                                            valueForm.displayValue
                                                        }
                                                        onChange={(
                                                            event,
                                                        ) =>
                                                            setValueForm(
                                                                (
                                                                    current,
                                                                ) => ({
                                                                    ...current,
                                                                    displayValue:
                                                                        event
                                                                            .target
                                                                            .value,
                                                                }),
                                                            )
                                                        }
                                                        placeholder={t(
                                                            'catalogAttributes.values.displayValue',
                                                        )}
                                                    />

                                                    <Input
                                                        value={
                                                            valueForm.colorHex
                                                        }
                                                        onChange={(
                                                            event,
                                                        ) =>
                                                            setValueForm(
                                                                (
                                                                    current,
                                                                ) => ({
                                                                    ...current,
                                                                    colorHex:
                                                                        event
                                                                            .target
                                                                            .value,
                                                                }),
                                                            )
                                                        }
                                                        placeholder="#FF0000"
                                                    />

                                                    <Input
                                                        type="number"
                                                        min={0}
                                                        value={
                                                            valueForm.displayOrder
                                                        }
                                                        onChange={(
                                                            event,
                                                        ) =>
                                                            setValueForm(
                                                                (
                                                                    current,
                                                                ) => ({
                                                                    ...current,
                                                                    displayOrder:
                                                                        Number(
                                                                            event
                                                                                .target
                                                                                .value,
                                                                        ),
                                                                }),
                                                            )
                                                        }
                                                        placeholder={t(
                                                            'catalogAttributes.values.displayOrder',
                                                        )}
                                                    />

                                                    <label className="flex items-center gap-2 text-sm">
                                                        <input
                                                            type="checkbox"
                                                            checked={
                                                                valueForm.isActive
                                                            }
                                                            onChange={(
                                                                event,
                                                            ) =>
                                                                setValueForm(
                                                                    (
                                                                        current,
                                                                    ) => ({
                                                                        ...current,
                                                                        isActive:
                                                                            event
                                                                                .target
                                                                                .checked,
                                                                    }),
                                                                )
                                                            }
                                                        />

                                                        {t(
                                                            'catalogAttributes.values.active',
                                                        )}
                                                    </label>

                                                    <div className="flex gap-2">
                                                        <Button
                                                            type="button"
                                                            onClick={() =>
                                                                submitValue(
                                                                    attribute.id,
                                                                )
                                                            }
                                                            disabled={
                                                                addValue.isPending ||
                                                                updateValue.isPending
                                                            }
                                                        >
                                                            {editingValueId
                                                                ? t(
                                                                      'catalogAttributes.values.save',
                                                                  )
                                                                : t(
                                                                      'catalogAttributes.values.create',
                                                                  )}
                                                        </Button>

                                                        <Button
                                                            type="button"
                                                            variant="ghost"
                                                            onClick={
                                                                cancelValueEdit
                                                            }
                                                        >
                                                            {t(
                                                                'catalogAttributes.values.cancel',
                                                            )}
                                                        </Button>
                                                    </div>
                                                </div>
                                            </div>
                                        )}

                                        {/* Values */}
                                        <div className="divide-y rounded-lg border">
                                            {attribute.values.length ===
                                                0 && (
                                                <div className="text-muted-foreground p-6 text-center text-sm">
                                                    {t(
                                                        'catalogAttributes.values.empty',
                                                    )}
                                                </div>
                                            )}

                                            {attribute.values.map(
                                                (value) => (
                                                    <div
                                                        key={
                                                            value.id
                                                        }
                                                        className="flex flex-col gap-3 p-4 md:flex-row md:items-center md:justify-between"
                                                    >
                                                        <div className="flex items-center gap-3">
                                                            {value.colorHex && (
                                                                <span
                                                                    className="size-5 rounded-full border"
                                                                    style={{
                                                                        backgroundColor:
                                                                            value.colorHex,
                                                                    }}
                                                                />
                                                            )}

                                                            <div>
                                                                <div className="font-medium">
                                                                    {
                                                                        value.value
                                                                    }
                                                                </div>

                                                                {value.displayValue && (
                                                                    <div className="text-muted-foreground text-xs">
                                                                        {
                                                                            value.displayValue
                                                                        }
                                                                    </div>
                                                                )}
                                                            </div>
                                                        </div>

                                                        <div className="flex flex-wrap items-center gap-2">
                                                            <span className="text-muted-foreground text-xs">
                                                                {t(
                                                                    'catalogAttributes.values.order',
                                                                )}{' '}
                                                                {
                                                                    value.displayOrder
                                                                }
                                                            </span>

                                                            <span className="text-muted-foreground text-xs">
                                                                {value.isActive
                                                                    ? t(
                                                                          'catalogAttributes.list.active',
                                                                      )
                                                                    : t(
                                                                          'catalogAttributes.list.inactive',
                                                                      )}
                                                            </span>

                                                            <Button
                                                                type="button"
                                                                variant="outline"
                                                                size="sm"
                                                                onClick={() =>
                                                                    startEditValue(
                                                                        value,
                                                                    )
                                                                }
                                                            >
                                                                <Pencil className="me-1 size-4" />

                                                                {t(
                                                                    'catalogAttributes.list.edit',
                                                                )}
                                                            </Button>

                                                            <Button
                                                                type="button"
                                                                variant="destructive"
                                                                size="sm"
                                                                onClick={() =>
                                                                    handleDeleteValue(
                                                                        attribute.id,
                                                                        value.id,
                                                                        value.value,
                                                                    )
                                                                }
                                                            >
                                                                <Trash2 className="me-1 size-4" />

                                                                {t(
                                                                    'catalogAttributes.list.delete',
                                                                )}
                                                            </Button>

                                                            {value.isActive ? (
                                                                <Power className="size-4 text-green-600" />
                                                            ) : (
                                                                <PowerOff className="text-muted-foreground size-4" />
                                                            )}
                                                        </div>
                                                    </div>
                                                ),
                                            )}
                                        </div>
                                    </CardContent>
                                )}
                            </Card>
                        );
                    },
                )}
            </div>

            {/* Empty State */}
            {sortedAttributes.length === 0 && (
                <Card>
                    <CardContent className="text-muted-foreground py-12 text-center">
                        {t(
                            'catalogAttributes.list.empty',
                        )}
                    </CardContent>
                </Card>
            )}
        </div>
    );
}

