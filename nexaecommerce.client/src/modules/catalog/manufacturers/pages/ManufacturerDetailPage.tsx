// src/modules/catalog/manufacturers/pages/ManufacturerDetailPage.tsx
import { useNavigate, useParams } from 'react-router-dom';
import { ArrowLeft, Pencil, Globe, Calendar, Link as LinkIcon, Star } from 'lucide-react';

import { PageHeader, ErrorState, LoadingSkeleton } from '@/components/data-states';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { useManufacturer } from '@/modules/catalog/manufacturers/hooks';
import { useDocumentTitle } from '@/hooks/use-document-title';
// ✅ تغییر به date.ts
import { formatDate } from '@/lib/date';

export default function ManufacturerDetailPage() {
    useDocumentTitle('جزئیات تولیدکننده');
    const { id } = useParams<{ id: string }>();
    const navigate = useNavigate();
    const { data: manufacturer, isLoading, isError, refetch } = useManufacturer(id);

    if (isLoading) return <LoadingSkeleton variant="cards" rows={4} />;
    if (isError || !manufacturer) return <ErrorState error={new Error('تولیدکننده یافت نشد')} onRetry={() => refetch()} message="بارگذاری تولیدکننده با خطا مواجه شد." />;

    return (
        <div className="space-y-6">
            <PageHeader
                title={manufacturer.name}
                description={`مدیریت تولیدکننده ${manufacturer.name}`}
                actions={
                    <div className="flex gap-2">
                        <Button variant="outline" onClick={() => navigate('/admin/manufacturers')}>
                            <ArrowLeft /> بازگشت
                        </Button>
                        <Button onClick={() => navigate(`/admin/manufacturers/${id}/edit`)}>
                            <Pencil /> ویرایش
                        </Button>
                    </div>
                }
            />

            <div className="grid gap-6 md:grid-cols-2">
                {/* اطلاعات اصلی */}
                <Card>
                    <CardHeader>
                        <CardTitle>اطلاعات اصلی</CardTitle>
                        <CardDescription>اطلاعات پایه تولیدکننده</CardDescription>
                    </CardHeader>
                    <CardContent className="space-y-4">
                        <div className="flex items-center gap-4">
                            <div className="bg-muted grid size-16 shrink-0 place-items-center overflow-hidden rounded-lg border">
                                {manufacturer.logoUrl ? (
                                    <img src={manufacturer.logoUrl} alt={manufacturer.name} className="size-full object-contain" />
                                ) : (
                                    <Globe className="text-muted-foreground size-8" />
                                )}
                            </div>
                            <div>
                                <div className="text-lg font-semibold">{manufacturer.name}</div>
                                <div className="text-muted-foreground text-sm">Slug: {manufacturer.slug || '—'}</div>
                            </div>
                        </div>

                        <div className="space-y-2">
                            <div className="flex items-center gap-2 text-sm">
                                <Badge variant={manufacturer.isActive ? 'default' : 'secondary'}>
                                    {manufacturer.isActive ? 'فعال' : 'غیرفعال'}
                                </Badge>
                                <Badge variant={manufacturer.isPublished ? 'default' : 'outline'}>
                                    {manufacturer.isPublished ? 'منتشر شده' : 'پیش‌نویس'}
                                </Badge>
                                {manufacturer.isFeatured && (
                                    <Badge variant="default">
                                        <Star className="mr-1 size-3" /> ویژه
                                    </Badge>
                                )}
                            </div>
                            <div className="text-muted-foreground text-sm">
                                ترتیب نمایش: {manufacturer.displayOrder || 0}
                            </div>
                            {manufacturer.website && (
                                <div className="flex items-center gap-2 text-sm">
                                    <LinkIcon className="size-4" />
                                    <a href={manufacturer.website} target="_blank" rel="noopener noreferrer" className="text-primary hover:underline">
                                        {manufacturer.website}
                                    </a>
                                </div>
                            )}
                        </div>

                        {manufacturer.description && (
                            <div className="border-t pt-4">
                                <div className="text-sm font-medium">توضیحات</div>
                                <div className="text-muted-foreground text-sm mt-1">{manufacturer.description}</div>
                            </div>
                        )}
                    </CardContent>
                </Card>

                {/* تصاویر */}
                <Card>
                    <CardHeader>
                        <CardTitle>تصاویر</CardTitle>
                        <CardDescription>لوگو و تصویر کاور تولیدکننده</CardDescription>
                    </CardHeader>
                    <CardContent className="space-y-4">
                        {manufacturer.logoUrl && (
                            <div>
                                <div className="text-sm font-medium mb-2">لوگو</div>
                                <img src={manufacturer.logoUrl} alt={`${manufacturer.name} logo`} className="max-h-32 w-auto rounded-lg border object-contain" />
                            </div>
                        )}
                        {manufacturer.coverImageUrl && (
                            <div>
                                <div className="text-sm font-medium mb-2">تصویر کاور</div>
                                <img src={manufacturer.coverImageUrl} alt={`${manufacturer.name} cover`} className="max-h-48 w-full rounded-lg border object-cover" />
                            </div>
                        )}
                        {!manufacturer.logoUrl && !manufacturer.coverImageUrl && (
                            <div className="text-muted-foreground text-sm">هیچ تصویری آپلود نشده است.</div>
                        )}
                    </CardContent>
                </Card>

                {/* SEO */}
                <Card className="md:col-span-2">
                    <CardHeader>
                        <CardTitle>SEO</CardTitle>
                        <CardDescription>اطلاعات سئو برای صفحه تولیدکننده</CardDescription>
                    </CardHeader>
                    <CardContent className="space-y-4">
                        <div className="grid gap-4 md:grid-cols-2">
                            <div>
                                <div className="text-sm font-medium">عنوان سئو</div>
                                <div className="text-muted-foreground text-sm mt-1">{manufacturer.seoTitle || '—'}</div>
                            </div>
                            <div>
                                <div className="text-sm font-medium">کلمات کلیدی سئو</div>
                                <div className="text-muted-foreground text-sm mt-1">{manufacturer.seoKeywords || '—'}</div>
                            </div>
                        </div>
                        <div>
                            <div className="text-sm font-medium">توضیحات سئو</div>
                            <div className="text-muted-foreground text-sm mt-1">{manufacturer.seoDescription || '—'}</div>
                        </div>
                    </CardContent>
                </Card>

                {/* متادیتا */}
                <Card className="md:col-span-2">
                    <CardHeader>
                        <CardTitle>متادیتا</CardTitle>
                        <CardDescription>اطلاعات سیستمی تولیدکننده</CardDescription>
                    </CardHeader>
                    <CardContent className="grid gap-2 md:grid-cols-2">
                        <div className="flex items-center gap-2 text-sm">
                            <Calendar className="size-4" />
                            <span className="text-muted-foreground">ایجاد شده:</span>
                            <span>{formatDate(manufacturer.createdAt)}</span>
                        </div>
                        {manufacturer.updatedAt && (
                            <div className="flex items-center gap-2 text-sm">
                                <Calendar className="size-4" />
                                <span className="text-muted-foreground">آخرین بروزرسانی:</span>
                                <span>{formatDate(manufacturer.updatedAt)}</span>
                            </div>
                        )}
                    </CardContent>
                </Card>
            </div>
        </div>
    );
}