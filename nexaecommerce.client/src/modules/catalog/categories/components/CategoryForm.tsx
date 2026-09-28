// src/modules/catalog/categories/components/CategoryForm.tsx

import { useEffect } from 'react';
import { useTranslation } from 'react-i18next';
import { zodResolver } from '@hookform/resolvers/zod';
import { useForm } from 'react-hook-form';
import { z } from 'zod';
import {
    Loader2,
    Save,
} from 'lucide-react';

import { Button } from '@/components/ui/button';

import {
    Card,
    CardContent,
    CardDescription,
    CardHeader,
    CardTitle,
} from '@/components/ui/card';

import { Input } from '@/components/ui/input';

import { Textarea } from '@/components/ui/textarea';

import { Switch } from '@/components/ui/switch';

import { FileUpload } from '@/components/ui/file-upload';

import {
    Form,
    FormControl,
    FormField,
    FormItem,
    FormLabel,
    FormMessage,
} from '@/components/ui/form';

import { FormGrid } from '@/components/forms/form-grid';

import { FormBanner } from '@/components/auth/form-banner';

import { useSubmitForm } from '@/components/forms/use-submit-form';

import type {
    Category,
    CreateCategoryDto,
    UpdateCategoryDto,
} from '../../api/categories';

// =========================================================
// Validation
// =========================================================

const schema = z.object({
    name: z
        .string()
        .trim()
        .min(
            2,
            'Category name must contain at least 2 characters.',
        )
        .max(150),

    slug: z
        .string()
        .trim()
        .max(200)
        .optional(),

    description: z
        .string()
        .max(5000)
        .optional(),

    imageUrl: z
        .string()
        .optional(),

    // -----------------------------------------------------
    // SEO
    // -----------------------------------------------------

    seoTitle: z
        .string()
        .trim()
        .max(200)
        .optional(),

    seoDescription: z
        .string()
        .max(500)
        .optional(),

    seoKeywords: z
        .string()
        .max(1000)
        .optional(),

    // -----------------------------------------------------
    // Hierarchy
    // -----------------------------------------------------

    parentCategoryId: z
        .string()
        .optional()
        .nullable(),

    // -----------------------------------------------------
    // Display / Status
    // -----------------------------------------------------

    displayOrder: z
        .number()
        .int()
        .min(0)
        .max(2147483647),

    isActive: z.boolean(),

    isPublished: z.boolean(),

    isFeatured: z.boolean(),
});

type FormValues = z.infer<typeof schema>;

// =========================================================
// Defaults
// =========================================================

const emptyValues: FormValues = {
    name: '',

    slug: '',

    description: '',

    imageUrl: '',

    seoTitle: '',

    seoDescription: '',

    seoKeywords: '',

    parentCategoryId: null,

    displayOrder: 0,

    isActive: true,

    isPublished: false,

    isFeatured: false,
};

// =========================================================
// Map Category -> Form
// =========================================================

function toValues(
    category?: Category,
): FormValues {
    if (!category) {
        return {
            ...emptyValues,
        };
    }

    return {
        name:
            category.name ?? '',

        slug:
            category.slug ?? '',

        description:
            category.description ?? '',

        imageUrl:
            category.imageUrl ?? '',

        seoTitle:
            category.seoTitle ?? '',

        seoDescription:
            category.seoDescription ?? '',

        seoKeywords:
            category.seoKeywords ?? '',

        parentCategoryId:
            category.parentCategoryId || null,

        displayOrder:
            category.displayOrder ?? 0,

        isActive:
            category.isActive,

        isPublished:
            category.isPublished,

        isFeatured:
            category.isFeatured,
    };
}

// =========================================================
// Component
// =========================================================

export function CategoryForm({
    category,
    mode,
    pending,
    onSubmit,
    onCancel,
    parentCategories = [],
}: {
    category?: Category;

    mode: 'create' | 'edit';

    pending?: boolean;

    onSubmit: (
        body:
            | CreateCategoryDto
            | UpdateCategoryDto,
    ) => Promise<unknown>;

    onCancel: () => void;

    parentCategories?: Category[];
}) {
    const { t } =
        useTranslation();

    // =======================================================
    // Form
    // =======================================================

    const form =
        useForm<FormValues>({
            resolver:
                zodResolver(schema),

            defaultValues:
                toValues(category),

            mode: 'onBlur',
        });

    // =======================================================
    // Reset When Category Changes
    // =======================================================

    useEffect(() => {
        form.reset(
            toValues(category),
        );
    }, [category, form]);

    // =======================================================
    // Submit
    // =======================================================

    const submitFlow =
        useSubmitForm<
            FormValues,
            CreateCategoryDto | UpdateCategoryDto,
            unknown
        >({
            form,

            mutationFn:
                onSubmit,

            fields:
                Object.keys(
                    emptyValues,
                ) as (keyof FormValues)[],

            successMessage:
                mode === 'create'
                    ? t(
                        'catalogForms.categoryCreated',
                    )
                    : t(
                        'catalogForms.categoryUpdated',
                    ),

            onSuccess:
                onCancel,

            transform:
                (values) => {
                    const common = {
                        name:
                            values.name.trim(),

                        slug:
                            values.slug?.trim()
                            || undefined,

                        description:
                            values.description?.trim()
                            || null,

                        imageUrl:
                            values.imageUrl?.trim()
                            || null,

                        // SEO
                        seoTitle:
                            values.seoTitle?.trim()
                            || null,

                        seoDescription:
                            values.seoDescription?.trim()
                            || null,

                        seoKeywords:
                            values.seoKeywords?.trim()
                            || null,

                        // Hierarchy
                        parentCategoryId:
                            values.parentCategoryId
                            || null,

                        // Display / Status
                        displayOrder:
                            Number(
                                values.displayOrder,
                            ) || 0,

                        isActive:
                            values.isActive,

                        isPublished:
                            values.isPublished,

                        isFeatured:
                            values.isFeatured,
                    };

                    if (mode === 'create') {
                        return common;
                    }

                    return common;
                },
        });

    // =======================================================
    // Render
    // =======================================================

    return (
        <Form {...form}>
            <form
                onSubmit={
                    submitFlow.submit
                }
                className="space-y-6"
                noValidate
            >
                {/* =================================================
                    Submit Banner
                ================================================= */}

                {submitFlow.banner && (
                    <FormBanner
                        state={
                            submitFlow.banner
                        }
                    />
                )}

                {/* =================================================
                    Basic Information
                ================================================= */}

                <Card>
                    <CardHeader>
                        <CardTitle>
                            {t(
                                'catalogForms.basicInfo',
                            )}
                        </CardTitle>

                        <CardDescription>
                            {t(
                                'catalogForms.basicInfoCategory',
                            )}
                        </CardDescription>
                    </CardHeader>

                    <CardContent className="space-y-4">
                        <FormGrid columns={2}>
                            {/* =================================================
                                Name
                            ================================================= */}

                            <FormField
                                control={
                                    form.control
                                }
                                name="name"
                                render={({
                                    field,
                                }) => (
                                    <FormItem>
                                        <FormLabel>
                                            {t(
                                                'catalogForms.nameRequired',
                                            )}
                                        </FormLabel>

                                        <FormControl>
                                            <Input
                                                {...field}
                                                autoFocus
                                            />
                                        </FormControl>

                                        <FormMessage />
                                    </FormItem>
                                )}
                            />

                            {/* =================================================
                                Slug
                            ================================================= */}

                            <FormField
                                control={
                                    form.control
                                }
                                name="slug"
                                render={({
                                    field,
                                }) => (
                                    <FormItem>
                                        <FormLabel>
                                            {t(
                                                'catalogForms.slug',
                                            )}
                                        </FormLabel>

                                        <FormControl>
                                            <Input
                                                {...field}
                                                placeholder={
                                                    mode ===
                                                        'create'
                                                        ? t(
                                                            'catalogForms.autoSlug',
                                                        )
                                                        : undefined
                                                }
                                                disabled={
                                                    mode ===
                                                    'create'
                                                }
                                            />
                                        </FormControl>

                                        <FormMessage />
                                    </FormItem>
                                )}
                            />
                        </FormGrid>

                        {/* =================================================
                            Description
                        ================================================= */}

                        <FormField
                            control={
                                form.control
                            }
                            name="description"
                            render={({
                                field,
                            }) => (
                                <FormItem>
                                    <FormLabel>
                                        {t(
                                            'catalogForms.description',
                                        )}
                                    </FormLabel>

                                    <FormControl>
                                        <Textarea
                                            {...field}
                                            value={
                                                field.value ??
                                                ''
                                            }
                                            rows={5}
                                        />
                                    </FormControl>

                                    <FormMessage />
                                </FormItem>
                            )}
                        />

                        <FormGrid columns={2}>
                            {/* =================================================
                                Parent
                            ================================================= */}

                            <FormField
                                control={
                                    form.control
                                }
                                name="parentCategoryId"
                                render={({
                                    field,
                                }) => (
                                    <FormItem>
                                        <FormLabel>
                                            {t(
                                                'catalogForms.parentCategory',
                                            )}
                                        </FormLabel>

                                        <FormControl>
                                            <select
                                                className="
                                                    flex
                                                    h-10
                                                    w-full
                                                    rounded-md
                                                    border
                                                    border-input
                                                    bg-background
                                                    px-3
                                                    py-2
                                                    text-sm
                                                    ring-offset-background
                                                    focus-visible:outline-none
                                                    focus-visible:ring-2
                                                    focus-visible:ring-ring
                                                    focus-visible:ring-offset-2
                                                "
                                                value={
                                                    field.value ||
                                                    ''
                                                }
                                                onChange={(
                                                    event,
                                                ) =>
                                                    field.onChange(
                                                        event
                                                            .target
                                                            .value ||
                                                        null,
                                                    )
                                                }
                                            >
                                                <option value="">
                                                    {t(
                                                        'catalogForms.noParent',
                                                    )}
                                                </option>

                                                {parentCategories.map(
                                                    (
                                                        parent,
                                                    ) => (
                                                        <option
                                                            key={
                                                                parent.id
                                                            }
                                                            value={
                                                                parent.id
                                                            }
                                                        >
                                                            {
                                                                parent.name
                                                            }
                                                        </option>
                                                    ),
                                                )}
                                            </select>
                                        </FormControl>

                                        <FormMessage />
                                    </FormItem>
                                )}
                            />

                            {/* =================================================
                                Display Order
                            ================================================= */}

                            <FormField
                                control={
                                    form.control
                                }
                                name="displayOrder"
                                render={({
                                    field,
                                }) => (
                                    <FormItem>
                                        <FormLabel>
                                            {t(
                                                'catalogForms.displayOrder',
                                            )}
                                        </FormLabel>

                                        <FormControl>
                                            <Input
                                                {...field}
                                                type="number"
                                                min={0}
                                                onChange={(
                                                    event,
                                                ) =>
                                                    field.onChange(
                                                        event
                                                            .target
                                                            .valueAsNumber ||
                                                        0,
                                                    )
                                                }
                                            />
                                        </FormControl>

                                        <FormMessage />
                                    </FormItem>
                                )}
                            />
                        </FormGrid>
                    </CardContent>
                </Card>

                {/* =================================================
                    Category Image
                ================================================= */}

                <Card>
                    <CardHeader>
                        <CardTitle>
                            {t(
                                'catalogForms.image',
                            )}
                        </CardTitle>

                        <CardDescription>
                            {t(
                                'catalogForms.uploadImage',
                            )}
                        </CardDescription>
                    </CardHeader>

                    <CardContent>
                        <FormField
                            control={
                                form.control
                            }
                            name="imageUrl"
                            render={({
                                field,
                            }) => (
                                <FormItem>
                                    <FormLabel>
                                        {t(
                                            'catalogForms.image',
                                        )}
                                    </FormLabel>

                                    <FormControl>
                                        <FileUpload
                                            value={
                                                field.value ||
                                                ''
                                            }
                                            onChange={
                                                field.onChange
                                            }
                                            onRemove={() =>
                                                field.onChange(
                                                    '',
                                                )
                                            }
                                            accept="image/*"
                                            maxSize={
                                                5
                                            }
                                            label={t(
                                                'catalogForms.uploadImage',
                                            )}
                                            placeholder={t(
                                                'catalogForms.dropImage',
                                            )}
                                        />
                                    </FormControl>

                                    <FormMessage />
                                </FormItem>
                            )}
                        />
                    </CardContent>
                </Card>

                {/* =================================================
                    SEO
                ================================================= */}

                <Card>
                    <CardHeader>
                        <CardTitle>
                            {t(
                                'catalogForms.seo',
                            )}
                        </CardTitle>

                        <CardDescription>
                            {t(
                                'catalogForms.seoCategoryDesc',
                            )}
                        </CardDescription>
                    </CardHeader>

                    <CardContent className="space-y-4">
                        {/* =================================================
                            SEO Title
                        ================================================= */}

                        <FormField
                            control={
                                form.control
                            }
                            name="seoTitle"
                            render={({
                                field,
                            }) => (
                                <FormItem>
                                    <FormLabel>
                                        {t(
                                            'catalogForms.seoTitle',
                                        )}
                                    </FormLabel>

                                    <FormControl>
                                        <Input
                                            {...field}
                                            value={
                                                field.value ??
                                                ''
                                            }
                                            maxLength={
                                                200
                                            }
                                        />
                                    </FormControl>

                                    <FormMessage />
                                </FormItem>
                            )}
                        />

                        {/* =================================================
                            SEO Description
                        ================================================= */}

                        <FormField
                            control={
                                form.control
                            }
                            name="seoDescription"
                            render={({
                                field,
                            }) => (
                                <FormItem>
                                    <FormLabel>
                                        {t(
                                            'catalogForms.seoDescription',
                                        )}
                                    </FormLabel>

                                    <FormControl>
                                        <Textarea
                                            {...field}
                                            value={
                                                field.value ??
                                                ''
                                            }
                                            maxLength={
                                                500
                                            }
                                            rows={4}
                                        />
                                    </FormControl>

                                    <FormMessage />
                                </FormItem>
                            )}
                        />

                        {/* =================================================
                            SEO Keywords
                        ================================================= */}

                        <FormField
                            control={
                                form.control
                            }
                            name="seoKeywords"
                            render={({
                                field,
                            }) => (
                                <FormItem>
                                    <FormLabel>
                                        {t(
                                            'catalogForms.seoKeywords',
                                        )}
                                    </FormLabel>

                                    <FormControl>
                                        <Input
                                            {...field}
                                            value={
                                                field.value ??
                                                ''
                                            }
                                            maxLength={
                                                1000
                                            }
                                            placeholder={
                                                t(
                                                    'catalogForms.seoKeywordsCategoryPlaceholder',
                                                )
                                            }
                                        />
                                    </FormControl>

                                    <FormMessage />
                                </FormItem>
                            )}
                        />
                    </CardContent>
                </Card>

                {/* =================================================
                    Publishing
                ================================================= */}

                <Card>
                    <CardHeader>
                        <CardTitle>
                            {t(
                                'catalogForms.publishing',
                            )}
                        </CardTitle>

                        <CardDescription>
                            {t(
                                'catalogForms.categoryPublishingDesc',
                            )}
                        </CardDescription>
                    </CardHeader>

                    <CardContent className="grid gap-4 sm:grid-cols-3">
                        {([
                            [
                                'isActive',
                                t(
                                    'catalogForms.active',
                                ),
                                t(
                                    'catalogForms.inactiveHintCategory',
                                ),
                            ],

                            [
                                'isPublished',
                                t(
                                    'catalogForms.published',
                                ),
                                t(
                                    'catalogForms.publishedHintCategory',
                                ),
                            ],

                            [
                                'isFeatured',
                                t(
                                    'catalogForms.featured',
                                ),
                                t(
                                    'catalogForms.featuredHintCategory',
                                ),
                            ],
                        ] as const).map(
                            ([
                                name,
                                label,
                                description,
                            ]) => (
                                <FormField
                                    key={name}
                                    control={
                                        form.control
                                    }
                                    name={name}
                                    render={({
                                        field,
                                    }) => (
                                        <FormItem className="flex items-center justify-between rounded-lg border p-4">
                                            <div className="space-y-1">
                                                <FormLabel>
                                                    {
                                                        label
                                                    }
                                                </FormLabel>

                                                <p className="text-muted-foreground text-xs">
                                                    {
                                                        description
                                                    }
                                                </p>
                                            </div>

                                            <FormControl>
                                                <Switch
                                                    checked={
                                                        field.value
                                                    }
                                                    onCheckedChange={
                                                        field.onChange
                                                    }
                                                />
                                            </FormControl>
                                        </FormItem>
                                    )}
                                />
                            ),
                        )}
                    </CardContent>
                </Card>

                {/* =================================================
                    Actions
                ================================================= */}

                <div className="flex flex-wrap justify-end gap-2">
                    <Button
                        type="button"
                        variant="outline"
                        onClick={
                            onCancel
                        }
                        disabled={
                            pending ||
                            submitFlow.isPending
                        }
                    >
                        {t(
                            'catalogForms.cancel',
                        )}
                    </Button>

                    <Button
                        type="submit"
                        disabled={
                            pending ||
                            submitFlow.isPending
                        }
                    >
                        {submitFlow.isPending ? (
                            <Loader2 className="animate-spin" />
                        ) : (
                            <Save />
                        )}

                        {mode === 'create'
                            ? t(
                                'catalogForms.createCategory',
                            )
                            : t(
                                'catalogForms.updateCategory',
                            )}
                    </Button>
                </div>
            </form>
        </Form>
    );
}