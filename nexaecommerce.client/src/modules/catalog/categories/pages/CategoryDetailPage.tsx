// src/modules/catalog/categories/pages/CategoryDetailPage.tsx
import { useNavigate, useParams } from 'react-router-dom';
import { ArrowLeft, Pencil, FolderTree, Calendar, Star } from 'lucide-react';

import { PageHeader, ErrorState, LoadingSkeleton } from '@/components/data-states';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { useCategory } from '../hooks';
import { useDocumentTitle } from '@/hooks/use-document-title';
import { formatDate } from '@/lib/date';

export default function CategoryDetailPage() {
    useDocumentTitle('جزئیات دسته‌بندی');
    const { id } = useParams<{ id: string }>();
    const navigate = useNavigate();
    const { data: category, isLoading, isError, refetch } = useCategory(id);

    if (isLoading) return <LoadingSkeleton variant="cards" rows={4} />;
    if (isError || !category) return <ErrorState error={new Error('دسته‌بندی یافت نشد')} onRetry={() => refetch()} message="بارگذاری دسته‌بندی با خطا مواجه شد." />;

    return (
        <div className="space-y-6">
            <PageHeader
                title={category.name}
                description={`مدیریت دسته‌بندی ${category.name}`}
                actions={
                    <div className="flex gap-2">
                        <Button variant="outline" onClick={() => navigate('/admin/categories')}>
                            <ArrowLeft /> بازگشت
                        </Button>
                        <Button onClick={() => navigate(`/admin/categories/${id}/edit`)}>
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
                        <CardDescription>اطلاعات پایه دسته‌بندی</CardDescription>
                    </CardHeader>
                    <CardContent className="space-y-4">
                        <div className="flex items-center gap-4">
                            <div className="bg-muted grid size-16 shrink-0 place-items-center overflow-hidden rounded-lg border">
                                {category.imageUrl ? (
                                    <img src={category.imageUrl} alt={category.name} className="size-full object-contain" />
                                ) : (
                                    <FolderTree className="text-muted-foreground size-8" />
                                )}
                            </div>
                            <div>
                                <div className="text-lg font-semibold">{category.name}</div>
                                <div className="text-muted-foreground text-sm">Slug: {category.slug || '—'}</div>
                            </div>
                        </div>

                        <div className="space-y-2">
                            <div className="flex items-center gap-2 text-sm">
                                <Badge variant={category.isActive ? 'default' : 'secondary'}>
                                    {category.isActive ? 'فعال' : 'غیرفعال'}
                                </Badge>
                                <Badge variant={category.isPublished ? 'default' : 'outline'}>
                                    {category.isPublished ? 'منتشر شده' : 'پیش‌نویس'}
                                </Badge>
                                {category.isFeatured && (
                                    <Badge variant="default">
                                        <Star className="mr-1 size-3" /> ویژه
                                    </Badge>
                                )}
                            </div>
                            <div className="text-muted-foreground text-sm">
                                ترتیب نمایش: {category.displayOrder || 0}
                            </div>
                            {category.parentCategoryName && (
                                <div className="flex items-center gap-2 text-sm">
                                    <FolderTree className="size-4" />
                                    <span className="text-muted-foreground">دسته‌بندی والد:</span>
                                    <span>{category.parentCategoryName}</span>
                                </div>
                            )}
                        </div>

                        {category.description && (
                            <div className="border-t pt-4">
                                <div className="text-sm font-medium">توضیحات</div>
                                <div className="text-muted-foreground text-sm mt-1">{category.description}</div>
                            </div>
                        )}
                    </CardContent>
                </Card>

                {/* تصویر */}
                <Card>
                    <CardHeader>
                        <CardTitle>تصویر</CardTitle>
                        <CardDescription>تصویر دسته‌بندی</CardDescription>
                    </CardHeader>
                    <CardContent>
                        {category.imageUrl ? (
                            <img src={category.imageUrl} alt={category.name} className="max-h-48 w-auto rounded-lg border object-contain" />
                        ) : (
                            <div className="text-muted-foreground text-sm">هیچ تصویری آپلود نشده است.</div>
                        )}
                    </CardContent>
                </Card>

                {/* متادیتا */}
                <Card className="md:col-span-2">
                    <CardHeader>
                        <CardTitle>متادیتا</CardTitle>
                        <CardDescription>اطلاعات سیستمی دسته‌بندی</CardDescription>
                    </CardHeader>
                    <CardContent className="grid gap-2 md:grid-cols-2">
                        <div className="flex items-center gap-2 text-sm">
                            <Calendar className="size-4" />
                            <span className="text-muted-foreground">ایجاد شده:</span>
                            <span>{formatDate(category.createdAt)}</span>
                        </div>
                        {category.updatedAt && (
                            <div className="flex items-center gap-2 text-sm">
                                <Calendar className="size-4" />
                                <span className="text-muted-foreground">آخرین بروزرسانی:</span>
                                <span>{formatDate(category.updatedAt)}</span>
                            </div>
                        )}
                    </CardContent>
                </Card>
            </div>
        </div>
    );
}