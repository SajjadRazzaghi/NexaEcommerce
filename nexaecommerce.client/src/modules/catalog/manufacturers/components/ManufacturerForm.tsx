// src/modules/catalog/manufacturers/components/ManufacturerForm.tsx
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
import type { ManufacturerDetails, CreateManufacturerDto, UpdateManufacturerDto } from '@/modules/catalog/api/manufacturers';

const schema = z.object({
    name: z.string().trim().min(2, 'Name must contain at least 2 characters.').max(150),
    slug: z.string().trim().max(200).optional(),
    description: z.string().max(5000).optional(),
    website: z.string().url('Enter a valid URL.').or(z.literal('')).optional(),
    logoUrl: z.string().optional(),
    coverImageUrl: z.string().optional(),
    seoTitle: z.string().max(200).optional(),
    seoDescription: z.string().max(500).optional(),
    seoKeywords: z.string().max(1000).optional(),
    isActive: z.boolean(),
    isPublished: z.boolean(),
    isFeatured: z.boolean(),
    displayOrder: z.number().int().min(0).max(2147483647),
});

type FormValues = z.infer<typeof schema>;

const emptyValues: FormValues = {
    name: '', slug: '', description: '', website: '', logoUrl: '', coverImageUrl: '',
    seoTitle: '', seoDescription: '', seoKeywords: '',
    isActive: true, isPublished: false, isFeatured: false, displayOrder: 0,
};

function toValues(manufacturer?: ManufacturerDetails): FormValues {
    if (!manufacturer) return emptyValues;
    return {
        name: manufacturer.name ?? '',
        slug: manufacturer.slug ?? '',
        description: manufacturer.description ?? '',
        website: manufacturer.website ?? '',
        logoUrl: manufacturer.logoUrl ?? '',
        coverImageUrl: manufacturer.coverImageUrl ?? '',
        seoTitle: manufacturer.seoTitle ?? '',
        seoDescription: manufacturer.seoDescription ?? '',
        seoKeywords: manufacturer.seoKeywords ?? '',
        isActive: manufacturer.isActive,
        isPublished: manufacturer.isPublished,
        isFeatured: manufacturer.isFeatured,
        displayOrder: manufacturer.displayOrder ?? 0,
    };
}

export function ManufacturerForm({
    manufacturer,
    mode,
    pending,
    onSubmit,
    onCancel,
}: {
    manufacturer?: ManufacturerDetails;
    mode: 'create' | 'edit';
    pending?: boolean;
    onSubmit: (body: CreateManufacturerDto | UpdateManufacturerDto) => Promise<unknown>;
    onCancel: () => void;
}) {
    const { t } = useTranslation();

    const form = useForm<FormValues>({
        resolver: zodResolver(schema),
        defaultValues: toValues(manufacturer),
        mode: 'onBlur',
    });

    useEffect(() => {
        form.reset(toValues(manufacturer));
    }, [manufacturer?.id]);

    const submitFlow = useSubmitForm<FormValues, CreateManufacturerDto | UpdateManufacturerDto, unknown>({
        form,
        mutationFn: onSubmit,
        fields: Object.keys(emptyValues) as (keyof FormValues)[],
        successMessage: mode === 'create' ? t('catalogForms.manufacturerCreated') : t('catalogForms.manufacturerUpdated'),
        onSuccess: onCancel,
        transform: (values) => {
            const common: CreateManufacturerDto = {
                name: values.name.trim(),
                description: values.description?.trim() || null,
                website: values.website?.trim() || null,
                logoUrl: values.logoUrl?.trim() || null,
                coverImageUrl: values.coverImageUrl?.trim() || null,
                seoTitle: values.seoTitle?.trim() || null,
                seoDescription: values.seoDescription?.trim() || null,
                seoKeywords: values.seoKeywords?.trim() || null,
            };

            if (mode === 'create') return common;

            return {
                ...common,
                slug: values.slug?.trim() || null,
                isActive: values.isActive,
                isPublished: values.isPublished,
                isFeatured: values.isFeatured,
                displayOrder: Number(values.displayOrder) || 0,
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
                        <CardDescription>{t('catalogForms.basicInfoManufacturer')}</CardDescription>
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
                            <FormField control={form.control} name="website" render={({ field }) => (
                                <FormItem>
                                    <FormLabel>{t('catalogForms.website')}</FormLabel>
                                    <FormControl><Input {...field} value={field.value ?? ''} placeholder="https://example.com" /></FormControl>
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

                {/* تصاویر برند */}
                <Card>
                    <CardHeader>
                        <CardTitle>{t('catalogForms.manufacturerImages')}</CardTitle>
                        <CardDescription>{t('catalogForms.manufacturerImages')}</CardDescription>
                    </CardHeader>
                    <CardContent>
                        <FormGrid columns={2}>
                            <FormField
                                control={form.control}
                                name="logoUrl"
                                render={({ field }) => (
                                    <FormItem>
                                        <FormLabel>{t('catalogForms.uploadLogo')}</FormLabel>
                                        <FormControl>
                                            <FileUpload
                                                value={field.value || ''}
                                                onChange={field.onChange}
                                                onRemove={() => field.onChange('')}
                                                accept="image/*"
                                                maxSize={5}
                                                label={t('catalogForms.uploadLogo')}
                                                placeholder={t('catalogForms.dropLogo')}
                                            />
                                        </FormControl>
                                        <FormMessage />
                                    </FormItem>
                                )}
                            />
                            <FormField
                                control={form.control}
                                name="coverImageUrl"
                                render={({ field }) => (
                                    <FormItem>
                                        <FormLabel>{t('catalogForms.coverImage')}</FormLabel>
                                        <FormControl>
                                            <FileUpload
                                                value={field.value || ''}
                                                onChange={field.onChange}
                                                onRemove={() => field.onChange('')}
                                                accept="image/*"
                                                maxSize={5}
                                                label={t('catalogForms.uploadCover')}
                                                placeholder={t('catalogForms.dropCover')}
                                            />
                                        </FormControl>
                                        <FormMessage />
                                    </FormItem>
                                )}
                            />
                        </FormGrid>
                    </CardContent>
                </Card>

                <Card>
                    <CardHeader>
                        <CardTitle>SEO</CardTitle>
                        <CardDescription>{t('catalogForms.seoManufacturerDesc')}</CardDescription>
                    </CardHeader>
                    <CardContent className="space-y-4">
                        <FormField control={form.control} name="seoTitle" render={({ field }) => (
                            <FormItem>
                                <FormLabel>{t('catalogForms.seoTitle')}</FormLabel>
                                <FormControl><Input {...field} value={field.value ?? ''} /></FormControl>
                                <FormMessage />
                            </FormItem>
                        )} />
                        <FormField control={form.control} name="seoDescription" render={({ field }) => (
                            <FormItem>
                                <FormLabel>{t('catalogForms.seoDescription')}</FormLabel>
                                <FormControl><Textarea {...field} value={field.value ?? ''} rows={3} /></FormControl>
                                <FormMessage />
                            </FormItem>
                        )} />
                        <FormField control={form.control} name="seoKeywords" render={({ field }) => (
                            <FormItem>
                                <FormLabel>{t('catalogForms.seoKeywords')}</FormLabel>
                                <FormControl><Input {...field} value={field.value ?? ''} placeholder={t('catalogForms.seoKeywordsManufacturerPlaceholder')} /></FormControl>
                                <FormMessage />
                            </FormItem>
                        )} />
                    </CardContent>
                </Card>

                <Card>
                    <CardHeader>
                        <CardTitle>{t('catalogForms.publishing')}</CardTitle>
                        <CardDescription>{t('catalogForms.manufacturerPublishingDesc')}</CardDescription>
                    </CardHeader>
                    <CardContent className="grid gap-4 sm:grid-cols-3">
                        {([
                            ['isActive', t('catalogForms.active'), t('catalogForms.inactiveHintManufacturer')],
                            ['isPublished', t('catalogForms.published'), t('catalogForms.publishedHintManufacturer')],
                            ['isFeatured', t('catalogForms.featured'), t('catalogForms.featuredHintManufacturer')],
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
                        {mode === 'create' ? t('catalogForms.createManufacturer') : t('catalogForms.updateManufacturer')}
                    </Button>
                </div>
            </form>
        </Form>
    );
}