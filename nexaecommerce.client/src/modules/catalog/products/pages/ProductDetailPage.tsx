// src/modules/catalog/products/pages/ProductDetailPage.tsx
import { useNavigate, useParams } from 'react-router-dom';
import {
    ArrowLeft,
    Pencil,
    Package,
    Calendar,
    Tag,
    Building,
    Layers,
    Star,
    DollarSign
} from 'lucide-react';
import { PageHeader, ErrorState, LoadingSkeleton } from '@/components/data-states';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { useProduct } from '../hooks';
import { useDocumentTitle } from '@/hooks/use-document-title';
import { formatDate, formatPrice } from '@/lib/utils';

export default function ProductDetailPage() {
    useDocumentTitle('جزئیات محصول');
    const { id } = useParams<{ id: string }>();
    const navigate = useNavigate();
    const { data: product, isLoading, isError, refetch } = useProduct(id);

    if (isLoading) return <LoadingSkeleton variant="cards" rows={4} />;
    if (isError || !product) {
        return (
            <ErrorState
                error={new Error('محصول یافت نشد')}
                onRetry={() => refetch()}
                message="بارگذاری محصول با خطا مواجه شد."
            />
        );
    }

    return (
        <div className="space-y-6">
            <PageHeader
                title={product.name}
                description={`مدیریت محصول ${product.name}`}
                actions={
                    <div className="flex gap-2">
                        <Button variant="outline" onClick={() => navigate('/admin/products')}>
                            <ArrowLeft /> بازگشت
                        </Button>
                        <Button onClick={() => navigate(`/admin/products/${id}/edit`)}>
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
                        <CardDescription>اطلاعات پایه محصول</CardDescription>
                    </CardHeader>
                    <CardContent className="space-y-4">
                        <div className="flex items-center gap-4">
                            <div className="bg-muted grid size-16 shrink-0 place-items-center overflow-hidden rounded-lg border">
                                {product.images?.[0]?.imageUrl ? (
                                    <img src={product.images[0].imageUrl} alt={product.name} className="size-full object-contain" />
                                ) : (
                                    <Package className="text-muted-foreground size-8" />
                                )}
                            </div>
                            <div>
                                <div className="text-lg font-semibold">{product.name}</div>
                                <div className="text-muted-foreground text-sm">Slug: {product.slug || '—'}</div>
                                <div className="text-muted-foreground text-sm">SKU: {product.sku || '—'}</div>
                            </div>
                        </div>

                        <div className="space-y-2">
                            <div className="flex flex-wrap items-center gap-2 text-sm">
                                <Badge variant={product.isActive ? 'default' : 'secondary'}>
                                    {product.isActive ? 'فعال' : 'غیرفعال'}
                                </Badge>
                                <Badge variant={product.isFeatured ? 'default' : 'outline'}>
                                    {product.isFeatured ? 'ویژه' : 'عادی'}
                                </Badge>
                                <Badge variant={product.isInStock ? 'default' : 'destructive'}>
                                    {product.isInStock ? `موجود (${product.stockQuantity})` : 'ناموجود'}
                                </Badge>
                            </div>
                            {product.brandName && (
                                <div className="flex items-center gap-2 text-sm">
                                    <Building className="size-4" />
                                    <span className="text-muted-foreground">برند:</span>
                                    <span>{product.brandName}</span>
                                </div>
                            )}
                            {product.categories?.length > 0 && (
                                <div className="flex items-center gap-2 text-sm">
                                    <Layers className="size-4" />
                                    <span className="text-muted-foreground">دسته‌بندی‌ها:</span>
                                    <span>{product.categories.join(', ')}</span>
                                </div>
                            )}
                        </div>

                        {product.description && (
                            <div className="border-t pt-4">
                                <div className="text-sm font-medium">توضیحات</div>
                                <div className="text-muted-foreground text-sm mt-1 whitespace-pre-wrap">{product.description}</div>
                            </div>
                        )}
                    </CardContent>
                </Card>

                {/* قیمت و تخفیف */}
                <Card>
                    <CardHeader>
                        <CardTitle>قیمت و تخفیف</CardTitle>
                        <CardDescription>اطلاعات قیمت و تخفیف محصول</CardDescription>
                    </CardHeader>
                    <CardContent className="space-y-4">
                        <div className="flex items-center gap-2 text-lg">
                            <DollarSign className="size-5" />
                            <span className="font-bold">{formatPrice(product.finalPrice)}</span>
                            {product.price !== product.finalPrice && (
                                <span className="text-muted-foreground line-through text-sm">{formatPrice(product.price)}</span>
                            )}
                        </div>

                        {product.discountPercentage && (
                            <div className="flex items-center gap-2 text-sm">
                                <Tag className="size-4" />
                                <span className="text-muted-foreground">تخفیف:</span>
                                <Badge variant="destructive">{product.discountPercentage}%</Badge>
                                {product.discountStartDate && (
                                    <span className="text-muted-foreground text-xs">
                                        از {formatDate(product.discountStartDate)}
                                    </span>
                                )}
                                {product.discountEndDate && (
                                    <span className="text-muted-foreground text-xs">
                                        تا {formatDate(product.discountEndDate)}
                                    </span>
                                )}
                            </div>
                        )}
                    </CardContent>
                </Card>

                {/* واریانت‌ها */}
                {product.variants?.length > 0 && (
                    <Card className="md:col-span-2">
                        <CardHeader>
                            <CardTitle>واریانت‌ها</CardTitle>
                            <CardDescription>تنوع‌های محصول</CardDescription>
                        </CardHeader>
                        <CardContent>
                            <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
                                {product.variants.map((variant) => (
                                    <div key={variant.id} className="border rounded-lg p-4">
                                        <div className="flex items-center justify-between">
                                            <div>
                                                <div className="font-medium">{variant.sku}</div>
                                                <div className="text-sm text-muted-foreground">
                                                    {variant.color && <span>رنگ: {variant.color}</span>}
                                                    {variant.size && <span> • سایز: {variant.size}</span>}
                                                </div>
                                            </div>
                                            <Badge variant={variant.isActive ? 'default' : 'secondary'}>
                                                {variant.isActive ? 'فعال' : 'غیرفعال'}
                                            </Badge>
                                        </div>
                                        <div className="flex items-center justify-between mt-2">
                                            <span className="text-sm text-muted-foreground">موجودی: {variant.stockQuantity}</span>
                                            {variant.priceOverride && (
                                                <span className="text-sm font-medium">{formatPrice(variant.priceOverride)}</span>
                                            )}
                                        </div>
                                    </div>
                                ))}
                            </div>
                        </CardContent>
                    </Card>
                )}

                {/* تصاویر */}
                {product.images?.length > 0 && (
                    <Card className="md:col-span-2">
                        <CardHeader>
                            <CardTitle>تصاویر</CardTitle>
                            <CardDescription>تصاویر محصول</CardDescription>
                        </CardHeader>
                        <CardContent>
                            <div className="grid grid-cols-4 gap-4">
                                {product.images.map((image) => (
                                    <div key={image.id} className="relative border rounded-lg p-2">
                                        <img src={image.imageUrl} alt={image.altText || product.name} className="w-full h-32 object-cover rounded" />
                                        {image.isMain && (
                                            <span className="absolute top-1 left-1 bg-primary text-white text-xs px-1 rounded">اصلی</span>
                                        )}
                                    </div>
                                ))}
                            </div>
                        </CardContent>
                    </Card>
                )}

                {/* متادیتا */}
                <Card className="md:col-span-2">
                    <CardHeader>
                        <CardTitle>متادیتا</CardTitle>
                        <CardDescription>اطلاعات سیستمی محصول</CardDescription>
                    </CardHeader>
                    <CardContent className="grid gap-2 md:grid-cols-3">
                        <div className="flex items-center gap-2 text-sm">
                            <Calendar className="size-4" />
                            <span className="text-muted-foreground">ایجاد شده:</span>
                            <span>{formatDate(product.createdAt)}</span>
                        </div>
                        {product.updatedAt && (
                            <div className="flex items-center gap-2 text-sm">
                                <Calendar className="size-4" />
                                <span className="text-muted-foreground">آخرین بروزرسانی:</span>
                                <span>{formatDate(product.updatedAt)}</span>
                            </div>
                        )}
                        <div className="flex items-center gap-2 text-sm">
                            <Star className="size-4" />
                            <span className="text-muted-foreground">امتیاز:</span>
                            <span>{product.averageRating?.toFixed(1) || '—'} ({product.reviewCount || 0} نظر)</span>
                        </div>
                    </CardContent>
                </Card>
            </div>
        </div>
    );
}