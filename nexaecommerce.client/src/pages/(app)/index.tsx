import {
    useTranslation,
} from 'react-i18next';
import {
    useQuery,
} from '@tanstack/react-query';

import {
    ArrowLeft,
    ArrowRight,
    BadgeCheck,
    Heart,
    Image as ImageIcon,
    ShoppingBag,
    Sparkles,
} from 'lucide-react';

import Instagram from '@mui/icons-material/Instagram';

import {
    Link,
} from 'react-router-dom';

import {
    appearanceApi,
} from '@/lib/api/appearance';

import {
    useCategories,
} from '@/modules/catalog/hooks/useCategories';

import {
    useProducts,
} from '@/modules/catalog/hooks/useProducts';

import ProductCard from '@/modules/catalog/components/ProductCard';


const DEFAULT_STORE_NAME =
    'پاتلت';

const PATLET_INSTAGRAM_URL =
    'https://www.instagram.com/patlet.dress/';
function SectionHeading({
    title,
    subtitle,
    href,
}: {
    title: string;
    subtitle?: string;
    href?: string;
    }) {

    return (
        <div className="mb-5 flex items-end justify-between gap-4">
            <div>
                <h2 className="text-2xl font-black tracking-tight sm:text-3xl">
                    {title}
                </h2>

                {subtitle && (
                    <p className="mt-1 text-sm text-muted-foreground">
                        {subtitle}
                    </p>
                )}
            </div>

            {href && (
                <Link
                    to={href}
                    className="hidden items-center gap-1 text-sm font-bold text-primary sm:flex"
                >
                    View all

                    <ArrowLeft className="size-4 rtl:hidden" />

                    <ArrowRight className="hidden size-4 rtl:block" />
                </Link>
            )}
        </div>
    );
}

function Hero({
    storeName,
    logoUrl,
    galleryProducts,
}: {
    storeName: string;
    logoUrl: string | null;
    galleryProducts: Array<{
        id: string;
        name: string;
        slug: string;
        mainImage?: string | null;
    }>;
}) {
    const lead = galleryProducts[0];
    const supporting = galleryProducts.slice(1, 5);

    return (
        <section
            aria-labelledby="patlet-home-title"
            className="relative overflow-hidden rounded-[2.5rem] border bg-card shadow-sm"
        >
            {/* Decorative background */}
            <div
                aria-hidden="true"
                className="pointer-events-none absolute -right-28 -top-28 size-80 rounded-full bg-primary/10 blur-3xl"
            />

            <div
                aria-hidden="true"
                className="pointer-events-none absolute -bottom-32 -left-20 size-96 rounded-full bg-primary/5 blur-3xl"
            />

            <div className="relative grid lg:grid-cols-[0.88fr_1.12fr]">

                {/* Content */}
                <div className="flex flex-col justify-center p-6 sm:p-10 lg:p-14">

                    {/* Brand badge */}
                    <div className="inline-flex w-fit items-center gap-2 rounded-full border bg-background/85 px-3.5 py-2 text-xs font-extrabold tracking-wide shadow-sm">
                        {logoUrl ? (
                            <img
                                src={logoUrl}
                                alt=""
                                className="size-5 rounded-md object-contain"
                            />
                        ) : (
                            <Heart
                                className="size-4 text-primary"
                                aria-hidden="true"
                            />
                        )}

                        <span>
                            پاتلت دِرِس • لباس زنانه
                        </span>
                    </div>

                    {/* SEO primary heading */}
                    <h1
                        id="patlet-home-title"
                        className="mt-6 max-w-3xl text-4xl font-black leading-[1.08] tracking-tight sm:text-5xl lg:text-[3.8rem]"
                    >
                        لباس بارداری، ست زایمان و

                        <span className="block text-primary">
                            لباس راحتی زنانه
                        </span>
                    </h1>

                    {/* SEO supporting copy */}
                    <p className="mt-6 max-w-xl text-[0.98rem] leading-8 text-muted-foreground sm:text-lg">
                        در پاتلت مجموعه‌ای از لباس‌های بارداری،
                        ست‌های زایمان و لباس‌های راحتی زنانه را
                        با تمرکز بر راحتی، فرم زیبا و خرید آسان
                        آنلاین پیدا کنید.
                    </p>

                    {/* Main actions */}
                    <div className="mt-8 flex flex-wrap gap-3">

                        <Link
                            to="/products"
                            className="inline-flex min-h-12 items-center gap-2 rounded-2xl bg-primary px-6 py-3 font-extrabold text-primary-foreground shadow-lg shadow-primary/15 transition-all hover:-translate-y-0.5 hover:shadow-xl focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary focus-visible:ring-offset-2"
                        >
                            مشاهده محصولات

                            <ArrowLeft
                                className="size-4 rtl:hidden"
                                aria-hidden="true"
                            />

                            <ArrowRight
                                className="hidden size-4 rtl:block"
                                aria-hidden="true"
                            />
                        </Link>

                        <Link
                            to="/products?sortBy=newest"
                            className="inline-flex min-h-12 items-center rounded-2xl border bg-background px-6 py-3 font-extrabold transition-colors hover:bg-muted focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary focus-visible:ring-offset-2"
                        >
                            جدیدترین مدل‌ها
                        </Link>

                        <a
                            href={PATLET_INSTAGRAM_URL}
                            target="_blank"
                            rel="noopener noreferrer"
                            className="inline-flex min-h-12 items-center gap-2 rounded-2xl border border-primary/15 bg-primary/5 px-5 py-3 font-extrabold text-primary transition-colors hover:bg-primary/10 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary focus-visible:ring-offset-2"
                            aria-label="پاتلت در اینستاگرام"
                        >
                            <Instagram
                                sx={{
                                    fontSize: 18,
                                }}
                                aria-hidden="true"
                            />

                            patlet.dress
                        </a>
                    </div>

                    {/* Brand benefits */}
                    <div className="mt-9 grid max-w-xl grid-cols-1 gap-3 sm:grid-cols-3">

                        <div className="flex items-center gap-2.5 rounded-2xl border bg-background/65 px-3.5 py-3 text-sm font-bold">
                            <span className="grid size-8 shrink-0 place-items-center rounded-xl bg-primary/10 text-primary">
                                <Heart
                                    className="size-4"
                                    aria-hidden="true"
                                />
                            </span>

                            راحتی واقعی
                        </div>

                        <div className="flex items-center gap-2.5 rounded-2xl border bg-background/65 px-3.5 py-3 text-sm font-bold">
                            <span className="grid size-8 shrink-0 place-items-center rounded-xl bg-primary/10 text-primary">
                                <BadgeCheck
                                    className="size-4"
                                    aria-hidden="true"
                                />
                            </span>

                            انتخاب دقیق
                        </div>

                        <div className="flex items-center gap-2.5 rounded-2xl border bg-background/65 px-3.5 py-3 text-sm font-bold">
                            <span className="grid size-8 shrink-0 place-items-center rounded-xl bg-primary/10 text-primary">
                                <ShoppingBag
                                    className="size-4"
                                    aria-hidden="true"
                                />
                            </span>

                            خرید آسان
                        </div>
                    </div>
                </div>

                {/* Fashion gallery */}
                <div className="relative min-h-[470px] overflow-hidden border-t bg-muted/20 p-3 sm:min-h-[560px] sm:p-5 lg:min-h-[640px] lg:border-t-0 lg:border-s">

                    <div
                        aria-hidden="true"
                        className="absolute inset-0 bg-[radial-gradient(circle_at_top_left,rgba(255,255,255,0.8),transparent_42%)] dark:bg-[radial-gradient(circle_at_top_left,rgba(255,255,255,0.06),transparent_42%)]"
                    />

                    {galleryProducts.length > 0 ? (
                        <div className="relative grid min-h-[440px] grid-cols-[1.15fr_.85fr] grid-rows-2 gap-3 sm:min-h-[520px] lg:grid-cols-[1.28fr_.72fr]">

                            {/* Main product */}
                            <Link
                                to={'/products/' + lead.slug}
                                className="group relative row-span-2 min-h-0 overflow-hidden rounded-[2rem] border bg-background shadow-lg focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary focus-visible:ring-offset-2"
                            >
                                <img
                                    src={
                                        lead.mainImage ||
                                        '/placeholder.jpg'
                                    }
                                    alt={lead.name}
                                    className="absolute inset-0 h-full w-full object-cover transition-transform duration-700 group-hover:scale-[1.035]"
                                    loading="eager"
                                    fetchPriority="high"
                                    decoding="async"
                                />

                                <div className="absolute inset-0 bg-gradient-to-t from-black/65 via-black/10 to-transparent" />

                                <div className="absolute inset-x-0 bottom-0 p-5 text-white sm:p-6">

                                    <span className="inline-flex rounded-full border border-white/20 bg-black/20 px-3 py-1.5 text-[11px] font-extrabold backdrop-blur-md">
                                        انتخاب تازه پاتلت
                                    </span>

                                    <div className="mt-3 line-clamp-2 text-xl font-black leading-tight sm:text-2xl">
                                        {lead.name}
                                    </div>
                                </div>
                            </Link>

                            {/* Supporting products */}
                            {supporting.map(
                                (product, index) => (
                                    <Link
                                        key={product.id}
                                        to={
                                            '/products/' +
                                            product.slug
                                        }
                                        className="group relative min-h-0 overflow-hidden rounded-[1.5rem] border bg-background shadow-md focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary focus-visible:ring-offset-2"
                                    >
                                        <img
                                            src={
                                                product.mainImage ||
                                                '/placeholder.jpg'
                                            }
                                            alt={product.name}
                                            className="absolute inset-0 h-full w-full object-cover transition-transform duration-500 group-hover:scale-105"
                                            loading="lazy"
                                            decoding="async"
                                        />

                                        <div className="absolute inset-0 bg-gradient-to-t from-black/55 via-transparent to-transparent" />

                                        <div className="absolute inset-x-0 bottom-0 p-3 text-white">
                                            <div className="line-clamp-2 text-xs font-extrabold leading-5">
                                                {product.name}
                                            </div>
                                        </div>

                                        {index === 0 && (
                                            <span className="absolute start-3 top-3 rounded-full border border-white/20 bg-black/20 px-2.5 py-1 text-[10px] font-extrabold text-white backdrop-blur-md">
                                                منتخب
                                            </span>
                                        )}
                                    </Link>
                                ),
                            )}
                        </div>
                    ) : (
                        <div className="relative grid min-h-[440px] place-items-center overflow-hidden rounded-[2rem] border bg-gradient-to-br from-primary/10 via-background to-muted sm:min-h-[520px]">
                            {logoUrl ? (
                                <img
                                    src={logoUrl}
                                    alt={storeName}
                                    className="size-28 rounded-3xl object-contain"
                                />
                            ) : (
                                <ShoppingBag
                                    className="size-20 text-primary/45"
                                    aria-hidden="true"
                                />
                            )}
                        </div>
                    )}

                    <div className="absolute bottom-6 start-6 rounded-2xl border bg-background/90 px-4 py-3 text-xs font-bold shadow-lg backdrop-blur">
                        <div className="flex items-center gap-2">
                            <Sparkles
                                className="size-4 text-primary"
                                aria-hidden="true"
                            />

                            مدل‌های منتخب پاتلت • مشاهده محصول و انتخاب سایز
                        </div>
                    </div>
                </div>
            </div>
        </section>
    );
}
function CategorySection({
    categories,
}: {
    categories: Array<{
        id: string;
        name: string;
        slug?: string;
        imageUrl?: string;
        productCount?: number;
    }>;
}) {
    const { t } = useTranslation();

    if (categories.length === 0) {
        return null;
    }

    return (
        <section>
            <SectionHeading
                title={t(
                    'storefront.home.categories.title',
                )}
                subtitle={t(
                    'storefront.home.categories.subtitle',
                )}
            />

            <div className="grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-6">
                {categories
                    .slice(0, 6)
                    .map((category) => (
                        <Link
                            key={category.id}
                            to={`/products?categoryId=${encodeURIComponent(category.id)}`}
                            className="group overflow-hidden rounded-2xl border bg-card transition-all hover:-translate-y-1 hover:shadow-lg"
                        >
                            <div className="aspect-[1.15] overflow-hidden bg-muted">
                                {category.imageUrl ? (
                                    <img
                                        src={category.imageUrl}
                                        alt={category.name}
                                        className="h-full w-full object-cover transition-transform duration-300 group-hover:scale-105"
                                    />
                                ) : (
                                    <div className="flex h-full items-center justify-center bg-gradient-to-br from-primary/10 to-muted">
                                        <ShoppingBag className="size-10 text-primary/60" />
                                    </div>
                                )}
                            </div>

                            <div className="p-3">
                                <div className="line-clamp-1 font-bold">
                                    {category.name}
                                </div>

                                {typeof category.productCount ===
                                    'number' && (
                                        <div className="mt-1 text-xs text-muted-foreground">
                                            {category.productCount}{' '}
                                            {t(
                                                'storefront.home.categories.products',
                                                'products',
                                            )}
                                        </div>
                                    )}
                            </div>
                        </Link>
                    ))}
            </div>
        </section>
    );
}
function ProductSkeleton() {
    return (
        <div className="grid grid-cols-2 gap-4 sm:grid-cols-3 lg:grid-cols-4">
            {Array.from({
                length: 8,
            }).map((_, index) => (
                <div
                    key={index}
                    className="overflow-hidden rounded-2xl border"
                >
                    <div className="aspect-square animate-pulse bg-muted" />

                    <div className="space-y-3 p-4">
                        <div className="h-4 animate-pulse rounded bg-muted" />

                        <div className="h-4 w-2/3 animate-pulse rounded bg-muted" />

                        <div className="h-10 animate-pulse rounded-xl bg-muted" />
                    </div>
                </div>
            ))}
        </div>
    );
}

export default function StorefrontHomePage() {
    const {
        i18n,t
    } = useTranslation();
  
    const isFa =
        i18n.language
            ?.toLowerCase()
            .startsWith('fa');
    const {
        data: appearance,
    } = useQuery({
        queryKey: [
            'appearance',
        ],

        queryFn:
            appearanceApi.get,

        staleTime:
            5 * 60_000,

        retry: 1,
    });

    const {
        data: categories,
    } = useCategories();

    const {
        data: featured,
        isLoading: featuredLoading,
        isError: featuredError,
    } = useProducts({
        page: 1,
        pageSize: 8,
        isActive: true,
        isFeatured: true,
        sortBy: 'newest',
        desc: true,
    });

    const {
        data: latest,
        isLoading: latestLoading,
        isError: latestError,
    } = useProducts({
        page: 1,
        pageSize: 8,
        isActive: true,
        sortBy: 'newest',
        desc: true,
    });

    /*
     * Build the homepage fashion gallery only after
     * both featured and latest products are initialized.
     *
     * We prefer products with a real image and remove duplicates
     * so the hero gallery always contains visually useful items.
     */
    const gallerySource = [
        ...(featured?.items ?? []),
        ...(latest?.items ?? []),
    ];

    const galleryProducts: Array<{
        id: string;
        name: string;
        slug: string;
        mainImage?: string | null;
    }> = [];

    for (const product of gallerySource) {
        if (
            !product.mainImage ||
            galleryProducts.some(
                item => item.id === product.id,
            )
        ) {
            continue;
        }

        galleryProducts.push({
            id: product.id,
            name: product.name,
            slug: product.slug,
            mainImage: product.mainImage,
        });

        if (
            galleryProducts.length >= 5
        ) {
            break;
        }
    }

    const storeName =
        appearance?.storeName?.trim() ||
        DEFAULT_STORE_NAME;

    const logoUrl =
        appearance?.logoUrl?.trim() ||
        null;

    const contactPhone =
        appearance?.contactPhone?.trim() ||
        '';

    const contactEmail =
        appearance?.contactEmail?.trim() ||
        '';

    const websiteUrl =
        appearance?.websiteUrl?.trim() ||
        '';

    const contactAddress =
        appearance?.contactAddress?.trim() ||
        '';

    return (
        <div className="min-h-screen bg-background">
            <main className="mx-auto max-w-7xl space-y-12 px-4 py-6 sm:px-6 sm:py-8">

                {/* -------------------------------------------------
                    HERO / PATLET FASHION GALLERY
                   ------------------------------------------------- */}
                <Hero
                    storeName={storeName}
                    logoUrl={logoUrl}
                    galleryProducts={
                        galleryProducts
                    }
                />

                {/* -------------------------------------------------
                    CATEGORIES
                   ------------------------------------------------- */}
                <CategorySection
                    categories={
                        Array.isArray(categories)
                            ? categories
                            : []
                    }
                />

                {/* -------------------------------------------------
                    FEATURED PRODUCTS
                   ------------------------------------------------- */}
                <section>
                    <SectionHeading
                        title={t(
                            'storefront.home.featured.title',
                        )}
                        subtitle={t(
                            'storefront.home.featured.subtitle',
                        )}
                        href="/products"
                    />

                    {featuredLoading ? (
                        <ProductSkeleton />
                    ) : featuredError ? (
                        <div className="rounded-2xl border p-8 text-center text-sm text-muted-foreground">
                            Featured products are temporarily unavailable.
                        </div>
                    ) : featured?.items?.length ? (
                        <div className="grid grid-cols-2 gap-4 sm:grid-cols-3 lg:grid-cols-4">
                            {featured.items.map(
                                (
                                    product,
                                ) => (
                                    <ProductCard
                                        key={
                                            product.id
                                        }
                                        product={
                                            product
                                        }
                                    />
                                ),
                            )}
                        </div>
                    ) : (
                        <div className="rounded-2xl border p-10 text-center">
                            <Sparkles
                                className="mx-auto size-10 text-muted-foreground"
                            />

                            <p className="mt-3 font-semibold">
                                No featured products yet.
                            </p>
                        </div>
                    )}
                </section>

                {/* -------------------------------------------------
                    NEW ARRIVALS
                   ------------------------------------------------- */}
                <section>
                    <SectionHeading
                        title={t(
                            'storefront.home.latest.title',
                        )}
                        subtitle={t(
                            'storefront.home.latest.subtitle',
                        )}
                        href="/products?sortBy=newest"
                    />

                    {latestLoading ? (
                        <ProductSkeleton />
                    ) : latestError ? (
                        <div className="rounded-2xl border p-8 text-center text-sm text-muted-foreground">
                            Products are temporarily unavailable.
                        </div>
                    ) : latest?.items?.length ? (
                        <div className="grid grid-cols-2 gap-4 sm:grid-cols-3 lg:grid-cols-4">
                            {latest.items.map(
                                (
                                    product,
                                ) => (
                                    <ProductCard
                                        key={
                                            product.id
                                        }
                                        product={
                                            product
                                        }
                                    />
                                ),
                            )}
                        </div>
                    ) : (
                        <div className="rounded-2xl border p-10 text-center text-muted-foreground">
                            No products are available yet.
                        </div>
                    )}
                </section>

                {/* -------------------------------------------------
                    STORE BENEFITS
                   ------------------------------------------------- */}
                
                
                <section className="rounded-[2rem] border bg-muted/30 p-6 sm:p-8">
                    <div className="grid gap-6 sm:grid-cols-3">

                        {/* Secure shopping */}
                        <div>
                            <div className="font-black">
                                {t(
                                    'storefront.home.benefitSection.secureTitle',
                                )}
                            </div>

                            <p className="mt-1 text-sm text-muted-foreground">
                                {t(
                                    'storefront.home.benefitSection.secureDescription',
                                )}
                            </p>
                        </div>

                        {/* Real inventory */}
                        <div>
                            <div className="font-black">
                                {t(
                                    'storefront.home.benefitSection.inventoryTitle',
                                )}
                            </div>

                            <p className="mt-1 text-sm text-muted-foreground">
                                {t(
                                    'storefront.home.benefitSection.inventoryDescription',
                                )}
                            </p>
                        </div>

                        {/* Easy checkout */}
                        <div>
                            <div className="font-black">
                                {t(
                                    'storefront.home.benefitSection.checkoutTitle',
                                )}
                            </div>

                            <p className="mt-1 text-sm text-muted-foreground">
                                {t(
                                    'storefront.home.benefitSection.checkoutDescription',
                                )}
                            </p>
                        </div>

                    </div>
                </section>
                

            </main>

            {/* -----------------------------------------------------
                FOOTER
               ----------------------------------------------------- */}
           
            <footer className="border-t">
                <div className="mx-auto max-w-7xl px-4 py-8 text-sm text-muted-foreground sm:px-6">

                    <div className="grid gap-8 md:grid-cols-[1fr_auto]">

                        {/* Store information */}
                        <div className="min-w-0">

                            <div className="flex items-center gap-3">

                                {logoUrl ? (
                                    <img
                                        src={logoUrl}
                                        alt=""
                                        className="size-9 rounded-lg object-contain"
                                    />
                                ) : (
                                    <div className="grid size-9 place-items-center rounded-lg bg-primary text-primary-foreground">
                                        <ImageIcon className="size-4" />
                                    </div>
                                )}

                                <div className="min-w-0">

                                    <div className="truncate font-bold text-foreground">
                                        {storeName}
                                    </div>

                                    <div className="text-xs">
                                        © {new Date().getFullYear()} {storeName}
                                    </div>

                                </div>
                            </div>

                            {(contactPhone ||
                                contactEmail ||
                                contactAddress) && (
                                    <div className="mt-4 grid gap-1 text-xs">

                                        {contactPhone && (
                                            <span>
                                                {contactPhone}
                                            </span>
                                        )}

                                        {contactEmail && (
                                            <span>
                                                {contactEmail}
                                            </span>
                                        )}

                                        {contactAddress && (
                                            <span className="max-w-xl leading-6">
                                                {contactAddress}
                                            </span>
                                        )}

                                    </div>
                                )}

                        </div>

                        {/* Footer navigation */}
                        <div className="flex flex-col gap-2 md:items-end">

                            {websiteUrl && (
                                <a
                                    href={websiteUrl}
                                    target="_blank"
                                    rel="noreferrer"
                                    className="font-semibold text-foreground hover:text-primary"
                                >
                                    {t(
                                        'storefront.home.footer.visitWebsite',
                                    )}
                                </a>
                            )}

                            <Link
                                to="/products"
                                className="font-semibold text-foreground hover:text-primary"
                            >
                                {t(
                                    'storefront.home.footer.browseProducts',
                                )}
                            </Link>

                            <a
                                href="https://www.instagram.com/patlet.dress/"
                                target="_blank"
                                rel="noopener noreferrer"
                                className="inline-flex items-center gap-2 font-semibold text-foreground hover:text-primary"
                            >
                                <Instagram
                                    sx={{
                                        fontSize: 18,
                                    }}
                                    aria-hidden="true"
                                />

                                {t(
                                    'storefront.home.footer.instagram',
                                )}
                            </a>

                        </div>
                    </div>
                </div>
            </footer>
            

        </div>
    );
}
