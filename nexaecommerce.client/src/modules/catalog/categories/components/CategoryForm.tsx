// src/modules/catalog/categories/components/CategoryForm.tsx
import { useEffect } from 'react';
import { useTranslation } from 'react-i18next';
import { zodResolver } from '@hookform/resolvers/zod';
import { useForm } from 'react-hook-form';
import { z } from 'zod';
import { Loader2, Save } from 'lucide-react';

import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Textarea } from '@/components/ui/textarea';
import { Switch } from '@/components/ui/switch';
import { FileUpload } from '@/components/ui/file-upload';
import { Form, FormControl, FormField, FormItem, FormLabel, FormMessage } from '@/components/ui/form';
import { FormGrid } from '@/components/forms/form-grid';
import { FormBanner } from '@/components/auth/form-banner';
import { useSubmitForm } from '@/components/forms/use-submit-form';
import type { Category, CreateCategoryDto, UpdateCategoryDto } from '../../api/categories';


const schema = z.object({
    name: z.string().trim().min(2, 'Category name must contain at least 2 characters.').max(150),
    slug: z.string().trim().max(200).optional(),
    description: z.string().max(5000).optional(),
    imageUrl: z.string().optional(),
    parentCategoryId: z.string().optional().nullable(),
    displayOrder: z.number().int().min(0).max(2147483647),
    isActive: z.boolean(),
    isPublished: z.boolean(),
    isFeatured: z.boolean(),
});

type FormValues = z.infer<typeof schema>;

const emptyValues: FormValues = {
    name: '', slug: '', description: '', imageUrl: '', parentCategoryId: null,
    displayOrder: 0, isActive: true, isPublished: false, isFeatured: false,
};

function toValues(category?: Category): FormValues {
    if (!category) return emptyValues;
    return {
        name: category.name ?? '',
        slug: category.slug ?? '',
        description: category.description ?? '',
        imageUrl: category.imageUrl ?? '',
        parentCategoryId: category.parentCategoryId || null,
        displayOrder: category.displayOrder ?? 0,
        isActive: category.isActive,
        isPublished: category.isPublished,
        isFeatured: category.isFeatured,
    };
}

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
    onSubmit: (body: CreateCategoryDto | UpdateCategoryDto) => Promise<unknown>;
    onCancel: () => void;
    parentCategories?: Category[];
}) {
    const { t } = useTranslation();

    const form = useForm<FormValues>({
        resolver: zodResolver(schema),
        defaultValues: toValues(category),
        mode: 'onBlur',
    });

    useEffect(() => {
        form.reset(toValues(category));
    }, [category]);

    const submitFlow = useSubmitForm<FormValues, CreateCategoryDto | UpdateCategoryDto, unknown>({
        form,
        mutationFn: onSubmit,
        fields: Object.keys(emptyValues) as (keyof FormValues)[],
        successMessage: mode === 'create' ? t('catalogForms.categoryCreated') : t('catalogForms.categoryUpdated'),
        onSuccess: onCancel,
        transform: (values) => {
            const common = {
                name: values.name.trim(),
                description: values.description?.trim() || null,
                imageUrl: values.imageUrl?.trim() || null,
                parentCategoryId: values.parentCategoryId || null,
                displayOrder: Number(values.displayOrder) || 0,
                isActive: values.isActive,
                isPublished: values.isPublished,
                isFeatured: values.isFeatured,
            };

            if (mode === 'create') return common;

            return {
                ...common,
                slug: values.slug?.trim() || null,
            };
        },
    });

    return (
        <Form {...form}>
            <form onSubmit={submitFlow.submit} className="space-y-6" noValidate>
                {submitFlow.banner && <FormBanner state={submitFlow.banner} />}

                <Card>
                    <CardHeader>
                        <CardTitle>{t('catalogForms.basicInfo')}</CardTitle>
                        <CardDescription>{t('catalogForms.basicInfoCategory')}</CardDescription>
                    </CardHeader>
                    <CardContent className="space-y-4">
                        <FormGrid columns={2}>
                            <FormField control={form.control} name="name" render={({ field }) => (
                                <FormItem>
                                    <FormLabel>{t('catalogForms.nameRequired')}</FormLabel>
                                    <FormControl><Input {...field} autoFocus /></FormControl>
                                    <FormMessage />
                                </FormItem>
                            )} />
                            <FormField control={form.control} name="slug" render={({ field }) => (
                                <FormItem>
                                    <FormLabel>Slug</FormLabel>
                                    <FormControl><Input {...field} placeholder={mode === 'create' ? t('catalogForms.autoSlug') : undefined} disabled={mode === 'create'} /></FormControl>
                                    <FormMessage />
                                </FormItem>
                            )} />
                        </FormGrid>

                        <FormField control={form.control} name="description" render={({ field }) => (
                            <FormItem>
                                <FormLabel>{t('catalogForms.description')}</FormLabel>
                                <FormControl><Textarea {...field} value={field.value ?? ''} rows={5} /></FormControl>
                                <FormMessage />
                            </FormItem>
                        )} />

                        <FormGrid columns={2}>
                            <FormField control={form.control} name="parentCategoryId" render={({ field }) => (
                                <FormItem>
                                    <FormLabel>{t('catalogForms.parentCategory')}</FormLabel>
                                    <FormControl>
                                        <select
                                            className="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2"
                                            value={field.value || ''}
                                            onChange={(e) => field.onChange(e.target.value || null)}
                                        >
                                            <option value="">{t('catalogForms.noParent')}</option>
                                            {parentCategories.map((cat) => (
                                                <option key={cat.id} value={cat.id}>
                                                    {cat.name}
                                                </option>
                                            ))}
                                        </select>
                                    </FormControl>
                                    <FormMessage />
                                </FormItem>
                            )} />
                            <FormField control={form.control} name="displayOrder" render={({ field }) => (
                                <FormItem>
                                    <FormLabel>{t('catalogForms.displayOrder')}</FormLabel>
                                    <FormControl><Input {...field} type="number" min={0} onChange={(event) => field.onChange(event.target.valueAsNumber || 0)} /></FormControl>
                                    <FormMessage />
                                </FormItem>
                            )} />
                        </FormGrid>
                    </CardContent>
                </Card>

                <Card>
                    <CardHeader>
                        <CardTitle>{t('catalogForms.image')}</CardTitle>
                        <CardDescription>{t('catalogForms.uploadImage')}</CardDescription>
                    </CardHeader>
                    <CardContent>
                        <FormField control={form.control} name="imageUrl" render={({ field }) => (
                            <FormItem>
                                <FormLabel>{t('catalogForms.image')}</FormLabel>
                                <FormControl>
                                    <FileUpload
                                        value={field.value || ''}
                                        onChange={field.onChange}
                                        onRemove={() => field.onChange('')}
                                        accept="image/*"
                                        maxSize={5}
                                        label={t('catalogForms.uploadImage')}
                                        placeholder={t('catalogForms.dropImage')}
                                    />
                                </FormControl>
                                <FormMessage />
                            </FormItem>
                        )} />
                    </CardContent>
                </Card>

                <Card>
                    <CardHeader>
                        <CardTitle>{t('catalogForms.publishing')}</CardTitle>
                        <CardDescription>{t('catalogForms.categoryPublishingDesc')}</CardDescription>
                    </CardHeader>
                    <CardContent className="grid gap-4 sm:grid-cols-3">
                        {([
                            ['isActive', t('catalogForms.active'), t('catalogForms.inactiveHintCategory')],
                            ['isPublished', t('catalogForms.published'), t('catalogForms.publishedHintCategory')],
                            ['isFeatured', t('catalogForms.featured'), t('catalogForms.featuredHintCategory')],
                        ] as const).map(([name, label, description]) => (
                            <FormField key={name} control={form.control} name={name} render={({ field }) => (
                                <FormItem className="flex items-center justify-between rounded-lg border p-4">
                                    <div className="space-y-1">
                                        <FormLabel>{label}</FormLabel>
                                        <p className="text-muted-foreground text-xs">{description}</p>
                                    </div>
                                    <FormControl><Switch checked={field.value} onCheckedChange={field.onChange} /></FormControl>
                                </FormItem>
                            )} />
                        ))}
                    </CardContent>
                </Card>

                <div className="flex flex-wrap justify-end gap-2">
                    <Button type="button" variant="outline" onClick={onCancel} disabled={pending || submitFlow.isPending}>
                        {t('catalogForms.cancel')}
                    </Button>
                    <Button type="submit" disabled={pending || submitFlow.isPending}>
                        {submitFlow.isPending ? <Loader2 className="animate-spin" /> : <Save />}
                        {mode === 'create' ? t('catalogForms.createCategory') : t('catalogForms.updateCategory')}
                    </Button>
                </div>
            </form>
        </Form>
    );
}