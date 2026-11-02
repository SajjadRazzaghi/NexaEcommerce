import { useState } from 'react';
import { Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';

import {
    Heart,
    ShoppingCart,
    Eye,
    Check,
    Tag,
    PackageCheck,
} from 'lucide-react';

import { resolveMediaUrl } from '@/lib/media-url';
import { formatMoney } from '@/lib/formatMoney';

import {
    productsApi,
    type Product,
    type ProductListItem,
    type ProductVariant,
} from '../api/products';

import ProductVariantPickerDialog from './ProductVariantPickerDialog';
import CartUpsellDialog from './CartUpsellDialog';

import { useCartMutations } from '@/modules/cart/hooks/useCartMutations';

interface ProductCardProps {
    product: ProductListItem;
}

function getProductUrl(slug: string): string {
    return `/products/${encodeURIComponent(slug.trim())}`;
}

export default function ProductCard({
    product,
}: ProductCardProps) {
    const { i18n, t } = useTranslation();

    const isFa =
        i18n.language?.toLowerCase().startsWith('fa') ?? false;

    const [favorite, setFavorite] = useState(false);
    const [added, setAdded] = useState(false);

    const [errorMessage, setErrorMessage] = useState<string | null>(null);

    const [variantDialogOpen, setVariantDialogOpen] = useState(false);
    const [variantProduct, setVariantProduct] = useState<Product | null>(null);
    const [variantLoading, setVariantLoading] = useState(false);

    const [upsellOpen, setUpsellOpen] = useState(false);
    const [upsellProductId, setUpsellProductId] = useState<string | null>(null);

    const { add } = useCartMutations();

    // ---------------------------------------------------------
    // Price
    // ---------------------------------------------------------

    const price =
        typeof product.finalPrice === 'number'
            ? product.finalPrice
            : product.price;

    const hasDiscount =
        product.comparePrice != null &&
        product.comparePrice > price;

    const discountPercentage =
        product.discountPercentage > 0
            ? Math.round(product.discountPercentage)
            : hasDiscount && product.comparePrice != null
                ? Math.round(
                    ((product.comparePrice - price) /
                        product.comparePrice) * 100,
                )
                : 0;

    const image =
        resolveMediaUrl(product.mainImage) ||
        '/placeholder.jpg';

    const productUrl = getProductUrl(product.slug);

    const formatPrice = (value: number) =>
        formatMoney(value, product.currency, i18n.language);

    // ---------------------------------------------------------
    // Stock
    // ---------------------------------------------------------

    const stockLabel = product.isInStock
        ? t('storefront.product.inStock', {
            defaultValue: 'موجود در انبار',
        })
        : t('storefront.product.outOfStock', {
            defaultValue: 'ناموجود',
        });

    // ---------------------------------------------------------
    // Variant dialog
    // ---------------------------------------------------------

    const closeVariantDialog = () => {
        setVariantDialogOpen(false);
        setVariantProduct(null);
        setVariantLoading(false);
    };

    // Load complete product because ProductListItem
    // does not contain the complete variant collection.
    const handleAddToCart = async () => {
        if (
            !product.isInStock ||
            add.isPending ||
            variantLoading
        ) {
            return;
        }

        setErrorMessage(null);
        setVariantLoading(true);

        try {
            const fullProduct = await productsApi.getBySlug(
                product.slug,
            );

            const sellableVariants = fullProduct.variants.filter(
                (variant) =>
                    variant.isActive &&
                    variant.stockQuantity > 0,
            );

            if (sellableVariants.length === 0) {
                setErrorMessage(
                    isFa
                        ? 'این محصول در حال حاضر موجود نیست.'
                        : 'This product is currently unavailable.',
                );

                return;
            }

            setVariantProduct(fullProduct);
            setVariantDialogOpen(true);
        } catch {
            setErrorMessage(
                isFa
                    ? 'دریافت اطلاعات محصول ناموفق بود.'
                    : 'Could not load product details.',
            );
        } finally {
            setVariantLoading(false);
        }
    };

    // ---------------------------------------------------------
    // Add selected variant to cart
    // ---------------------------------------------------------

    const handleVariantConfirm = async (
        variant: ProductVariant,
    ) => {
        if (
            !variant.isActive ||
            variant.stockQuantity <= 0 ||
            add.isPending
        ) {
            return;
        }

        setErrorMessage(null);

        try {
            await add.mutateAsync({
                productVariantId: variant.id,
                quantity: 1,
            });

            const sourceProductId = variantProduct?.id;

            setAdded(true);
            closeVariantDialog();

            if (sourceProductId) {
                setUpsellProductId(sourceProductId);
                setUpsellOpen(true);
            }
        } catch {
            setErrorMessage(
                isFa
                    ? 'افزودن محصول به سبد خرید انجام نشد.'
                    : 'Could not add product to cart.',
            );
        }
    };

    // ---------------------------------------------------------
    // Translations
    // ---------------------------------------------------------

    const addLabel = t('storefront.product.addToCart', {
        defaultValue: 'افزودن به سبد خرید',
    });

    const addingLabel = t('storefront.product.adding', {
        defaultValue: 'در حال افزودن...',
    });

    const viewLabel = t('storefront.product.view', {
        defaultValue: 'مشاهده محصول',
    });

    const addedLabel = t('storefront.product.viewCart', {
        defaultValue: 'مشاهده سبد خرید',
    });

    // ---------------------------------------------------------
    // UI
    // ---------------------------------------------------------

    return (
        <>
            <article
                dir={isFa ? 'rtl' : 'ltr'}
                className="
                    group isolate relative
                    flex h-full min-w-0 flex-col
                    cursor-pointer overflow-hidden
                    rounded-2xl border border-slate-200
                    bg-white
                    transition-all duration-300
                    hover:-translate-y-1
                    hover:border-primary/25
                    hover:shadow-xl
                    hover:shadow-slate-200/70
                "
            >
                {/* -------------------------------------------------
                    Full card navigation

                    This link stays behind the card content.
                    Clicking the card opens product details.
                ------------------------------------------------- */}

                <Link
                    to={productUrl}
                    aria-label={`مشاهده جزئیات ${product.name}`}
                    className="
                        absolute inset-0 z-0
                        rounded-2xl
                        focus-visible:outline-none
                        focus-visible:ring-2
                        focus-visible:ring-primary
                    "
                >
                    <span className="sr-only">
                        {product.name}
                    </span>
                </Link>

                {/* -------------------------------------------------
                    Product image
                ------------------------------------------------- */}

                <div
                    className="
                        pointer-events-none
                        relative z-10
                        aspect-[4/5]
                        overflow-hidden
                        bg-[#F5F1EF]
                    "
                >
                    <img
                        src={image}
                        alt={product.name}
                        loading="lazy"
                        decoding="async"
                        className="
                            h-full w-full object-cover
                            transition-transform duration-700
                            group-hover:scale-105
                        "
                        onError={(event) => {
                            if (
                                !event.currentTarget.src.endsWith(
                                    '/placeholder.jpg',
                                )
                            ) {
                                event.currentTarget.src =
                                    '/placeholder.jpg';
                            }
                        }}
                    />

                    {/* Image overlay */}

                    <div
                        className="
                            pointer-events-none
                            absolute inset-0
                            bg-gradient-to-t
                            from-black/15 via-transparent
                            to-transparent
                            opacity-0
                            transition-opacity duration-300
                            group-hover:opacity-100
                        "
                    />

                    {/* Discount badge */}

                    {discountPercentage > 0 && (
                        <div
                            className="
                                pointer-events-none
                                absolute end-3 top-3 z-10
                                rounded-lg
                                bg-red-600
                                px-2.5 py-1.5
                                text-xs font-extrabold
                                text-white shadow-sm
                            "
                        >
                            {discountPercentage.toLocaleString(
                                isFa ? 'fa-IR' : 'en-US',
                            )}٪ تخفیف
                        </div>
                    )}

                    {/* Featured badge */}

                    {product.isFeatured && (
                        <div
                            className="
                                pointer-events-none
                                absolute start-3 top-3 z-10
                                rounded-lg
                                bg-white/95
                                px-2.5 py-1.5
                                text-[10px] font-bold
                                text-slate-700
                                shadow-sm backdrop-blur
                            "
                        >
                            پیشنهاد ویژه
                        </div>
                    )}

                    {/* Favorite */}

                    <button
                        type="button"
                        aria-label={
                            favorite
                                ? 'حذف از علاقه‌مندی‌ها'
                                : 'افزودن به علاقه‌مندی‌ها'
                        }
                        aria-pressed={favorite}
                        onClick={(event) => {
                            event.stopPropagation();

                            setFavorite((value) => !value);
                        }}
                        className={`
                            pointer-events-auto
                            absolute start-3 bottom-3 z-20
                            flex size-10
                            items-center justify-center
                            rounded-full
                            shadow-md backdrop-blur
                            transition
                            ${favorite
                                ? 'bg-red-50 text-red-600'
                                : 'bg-white/95 text-slate-600 hover:bg-white hover:text-red-500'
                            }
                        `}
                    >
                        <Heart
                            className="size-5"
                            fill={
                                favorite
                                    ? 'currentColor'
                                    : 'none'
                            }
                        />
                    </button>

                    {/* Out of stock overlay */}

                    {!product.isInStock && (
                        <div
                            className="
                                pointer-events-none
                                absolute inset-0 z-[5]
                                flex items-center justify-center
                                bg-white/65
                                backdrop-blur-[1px]
                            "
                        >
                            <span
                                className="
                                    rounded-xl
                                    bg-slate-900/90
                                    px-5 py-2.5
                                    text-sm font-bold
                                    text-white
                                "
                            >
                                ناموجود
                            </span>
                        </div>
                    )}

                    {/* Quick view - desktop only */}

                    <Link
                        to={productUrl}
                        onClick={(event) => event.stopPropagation()}
                        className="
                            pointer-events-auto
                            absolute bottom-3 end-3 z-20
                            hidden
                            items-center gap-2
                            rounded-xl
                            bg-white/95
                            px-4 py-2.5
                            text-xs font-bold
                            text-slate-800
                            shadow-md backdrop-blur
                            transition hover:bg-white
                            sm:flex
                        "
                    >
                        <Eye className="size-4" />
                        مشاهده محصول
                    </Link>
                </div>

                {/* -------------------------------------------------
                    Product information

                    pointer-events-none lets card background
                    link receive clicks on empty content areas.
                ------------------------------------------------- */}

                <div
                    className="
                        pointer-events-none
                        relative z-10
                        flex flex-1 flex-col
                        gap-2.5
                        p-3.5 sm:p-4
                    "
                >
                    {/* Brand */}

                    {product.brandName ? (
                        <div className="text-[11px] font-semibold text-slate-400">
                            {product.brandName}
                        </div>
                    ) : (
                        <div className="h-4" />
                    )}

                    {/* Product name */}

                    <div
                        className="
                            line-clamp-2
                            min-h-[46px]
                            text-sm font-bold leading-6
                            text-slate-800
                            sm:text-[15px]
                        "
                    >
                        {product.name}
                    </div>

                    {/* Categories */}

                    {product.categoryNames?.length > 0 && (
                        <div className="flex min-h-5 flex-wrap gap-1.5">
                            {product.categoryNames
                                .slice(0, 2)
                                .map((category, index) => (
                                    <span
                                        key={`${category}-${index}`}
                                        className="text-[10px] text-slate-400"
                                    >
                                        {category}
                                    </span>
                                ))}
                        </div>
                    )}

                    {/* Price and stock */}

                    <div className="mt-auto pt-2">

                        {/* Previous price */}

                        {hasDiscount &&
                            product.comparePrice != null && (
                                <div className="mb-1 flex items-center gap-2">
                                    <span className="text-xs text-slate-400 line-through">
                                        {formatPrice(
                                            product.comparePrice,
                                        )}
                                    </span>

                                    {discountPercentage > 0 && (
                                        <span
                                            className="
                                                rounded-md
                                                bg-red-50
                                                px-1.5 py-0.5
                                                text-[10px]
                                                font-bold
                                                text-red-600
                                            "
                                        >
                                            {discountPercentage}٪
                                        </span>
                                    )}
                                </div>
                            )}

                        {/* Current price */}

                        <div className="flex flex-wrap items-baseline gap-1.5">
                            <div className="flex items-baseline">
                                <span className="text-lg font-black tracking-tight text-slate-900">
                                    {formatPrice(price)}
                                </span>
                            </div>
                        </div>

                        {/* Stock status */}

                        <div className="mt-2 flex items-center gap-1.5">
                            {product.isInStock ? (
                                <PackageCheck className="size-3.5 text-emerald-600" />
                            ) : (
                                <Tag className="size-3.5 text-red-500" />
                            )}

                            <span
                                className={`
                                    text-[11px] font-semibold
                                    ${product.isInStock
                                        ? 'text-emerald-700'
                                        : 'text-red-600'
                                    }
                                `}
                            >
                                {stockLabel}
                            </span>
                        </div>
                    </div>

                    {/* Error message */}

                    {errorMessage && (
                        <div
                            role="alert"
                            className="
                                pointer-events-auto
                                rounded-lg
                                bg-red-50
                                px-3 py-2
                                text-xs leading-5
                                text-red-700
                            "
                        >
                            {errorMessage}
                        </div>
                    )}

                    {/* -------------------------------------------------
                        Actions

                        Mobile: only Add to cart.
                        Desktop: View + Add to cart.
                    ------------------------------------------------- */}

                    <div
                        className="
                            pointer-events-auto
                            relative z-20
                            mt-2 grid w-full min-w-0
                            grid-cols-1 gap-2
                            sm:grid-cols-[0.85fr_1.15fr]
                        "
                    >
                        {/* View product - desktop only */}

                        <Link
                            to={productUrl}
                            onClick={(event) => event.stopPropagation()}
                            className="
                                hidden sm:flex
                                min-h-10 min-w-0
                                items-center justify-center
                                gap-1.5
                                rounded-xl
                                border border-slate-200
                                px-2
                                text-xs font-bold
                                text-slate-700
                                transition
                                hover:border-primary/40
                                hover:bg-primary/5
                                hover:text-primary
                            "
                        >
                            <Eye className="size-4 shrink-0" />

                            <span>
                                {viewLabel}
                            </span>
                        </Link>

                        {/* Already added */}

                        {added ? (
                            <Link
                                to="/cart"
                                onClick={(event) => event.stopPropagation()}
                                className="
                                    flex min-h-11 w-full min-w-0
                                    items-center justify-center gap-2
                                    rounded-xl
                                    bg-emerald-600
                                    px-3
                                    text-xs font-bold
                                    text-white
                                    transition
                                    hover:bg-emerald-700
                                "
                            >
                                <Check className="size-4 shrink-0" />

                                <span className="sm:hidden">
                                    مشاهده سبد خرید
                                </span>

                                <span className="hidden sm:inline">
                                    {addedLabel}
                                </span>
                            </Link>
                        ) : (
                            /* Add to cart */

                            <button
                                type="button"
                                disabled={
                                    !product.isInStock ||
                                    add.isPending ||
                                    variantLoading
                                }
                                onClick={(event) => {
                                    event.stopPropagation();

                                    void handleAddToCart();
                                }}
                                className="
                                    flex min-h-11 w-full min-w-0
                                    items-center justify-center
                                    gap-2
                                    rounded-xl
                                    bg-primary
                                    px-3
                                    text-xs font-bold
                                    text-primary-foreground
                                    transition
                                    hover:opacity-90
                                    active:scale-[0.98]
                                    disabled:cursor-not-allowed
                                    disabled:opacity-50
                                "
                            >
                                <ShoppingCart className="size-4 shrink-0" />

                                {/* Mobile label */}

                                <span className="sm:hidden">
                                    {add.isPending || variantLoading
                                        ? 'در حال افزودن...'
                                        : 'افزودن به سبد خرید'}
                                </span>

                                {/* Desktop label */}

                                <span className="hidden sm:inline">
                                    {add.isPending || variantLoading
                                        ? addingLabel
                                        : addLabel}
                                </span>
                            </button>
                        )}
                    </div>
                </div>
            </article>

            {/* -----------------------------------------------------
                Product variant selector
            ----------------------------------------------------- */}

            <ProductVariantPickerDialog
                open={variantDialogOpen}
                product={variantProduct}
                loading={
                    variantLoading ||
                    add.isPending
                }
                onClose={closeVariantDialog}
                onConfirm={handleVariantConfirm}
            />

            {/* -----------------------------------------------------
                Product recommendations / Upsell
            ----------------------------------------------------- */}

            <CartUpsellDialog
                open={upsellOpen}
                sourceProductId={upsellProductId}
                onClose={() => {
                    setUpsellOpen(false);
                    setUpsellProductId(null);
                }}
            />
        </>
    );
}