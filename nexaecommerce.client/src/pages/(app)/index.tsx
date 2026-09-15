import {
    useQuery,
} from '@tanstack/react-query';

import {
    ArrowLeft,
    ArrowRight,
    Image as ImageIcon,
    ShoppingBag,
    Sparkles,
} from 'lucide-react';

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
    'NexaECommerce';

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
}: {
    storeName: string;
    logoUrl: string | null;
}) {
    return (
        <section className="overflow-hidden rounded-[2rem] border bg-gradient-to-br from-primary/10 via-background to-muted p-6 sm:p-10">
            <div className="grid items-center gap-8 lg:grid-cols-[1.2fr_.8fr]">
                <div className="max-w-2xl">
                    <div className="mb-4 inline-flex items-center gap-2 rounded-full border bg-background/80 px-3 py-1.5 text-xs font-bold">
                        {logoUrl ? (
                            <img
                                src={logoUrl}
                                alt=""
                                className="size-4 rounded object-contain"
                            />
                        ) : (
                            <Sparkles className="size-4 text-primary" />
                        )}

                        Welcome to {storeName}
                    </div>

                    <h1 className="text-4xl font-black tracking-tight sm:text-5xl lg:text-6xl">
                        Everything you need.

                        <span className="block text-primary">
                            In one place.
                        </span>
                    </h1>

                    <p className="mt-5 max-w-xl text-base leading-8 text-muted-foreground sm:text-lg">
                        Discover products, compare prices, choose your
                        variants and order online through a fast,
                        modern shopping experience.
                    </p>

                    <div className="mt-7 flex flex-wrap gap-3">
                        <Link
                            to="/products"
                            className="inline-flex items-center gap-2 rounded-xl bg-primary px-6 py-3 font-bold text-primary-foreground shadow-sm transition-transform hover:-translate-y-0.5"
                        >
                            Shop now

                            <ArrowLeft className="size-4 rtl:hidden" />

                            <ArrowRight className="hidden size-4 rtl:block" />
                        </Link>

                        <Link
                            to="/products?sortBy=newest"
                            className="rounded-xl border bg-background px-6 py-3 font-bold"
                        >
                            New arrivals
                        </Link>
                    </div>
                </div>

                <div className="hidden min-h-[280px] items-center justify-center lg:flex">
                    <div className="relative h-64 w-64">
                        <div className="absolute inset-0 rotate-6 rounded-[3rem] bg-primary/10" />

                        <div className="absolute inset-4 rounded-[2.5rem] border bg-background shadow-2xl" />

                        <div className="absolute inset-0 flex items-center justify-center">
                            {logoUrl ? (
                                <img
                                    src={logoUrl}
                                    alt=""
                                    className="size-36 rounded-[2rem] object-contain"
                                />
                            ) : (
                                <ShoppingBag className="size-28 text-primary" />
                            )}
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
    if (categories.length === 0) {
        return null;
    }

    return (
        <section>
            <SectionHeading
                title="Shop by category"
                subtitle="Start with a category"
            />

            <div className="grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-6">
                {categories
                    .slice(0, 6)
                    .map((category) => (
                        <Link
                            key={category.id}
                            to={`/products?categoryId=${encodeURIComponent(category.id,)}`}
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
                                        {category.productCount} products
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
        isInStock: true,
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
        isInStock: true,
        sortBy: 'newest',
        desc: true,
    });

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
                <Hero
                    storeName={storeName}
                    logoUrl={logoUrl}
                />

                <CategorySection
                    categories={
                        Array.isArray(categories)
                            ? categories
                            : []
                    }
                />

                <section>
                    <SectionHeading
                        title="Featured products"
                        subtitle="Our selected products for you"
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
                                (product) => (
                                    <ProductCard
                                        key={product.id}
                                        product={product}
                                    />
                                ),
                            )}
                        </div>
                    ) : (
                        <div className="rounded-2xl border p-10 text-center">
                            <Sparkles className="mx-auto size-10 text-muted-foreground" />

                            <p className="mt-3 font-semibold">
                                No featured products yet.
                            </p>
                        </div>
                    )}
                </section>

                <section>
                    <SectionHeading
                        title="New arrivals"
                        subtitle="The latest products added to the store"
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
                                (product) => (
                                    <ProductCard
                                        key={product.id}
                                        product={product}
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

                <section className="rounded-[2rem] border bg-muted/30 p-6 sm:p-8">
                    <div className="grid gap-6 sm:grid-cols-3">
                        <div>
                            <div className="font-black">
                                Secure shopping
                            </div>

                            <p className="mt-1 text-sm text-muted-foreground">
                                Your order and account data stay protected.
                            </p>
                        </div>

                        <div>
                            <div className="font-black">
                                Real inventory
                            </div>

                            <p className="mt-1 text-sm text-muted-foreground">
                                Availability is connected to the inventory module.
                            </p>
                        </div>

                        <div>
                            <div className="font-black">
                                Easy checkout
                            </div>

                            <p className="mt-1 text-sm text-muted-foreground">
                                Add your products to the cart and continue to checkout.
                            </p>
                        </div>
                    </div>
                </section>
            </main>

            <footer className="border-t">
                <div className="mx-auto max-w-7xl px-4 py-8 text-sm text-muted-foreground sm:px-6">
                    <div className="grid gap-8 md:grid-cols-[1fr_auto]">
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

                        <div className="flex flex-col gap-2 md:items-end">
                            {websiteUrl && (
                                <a
                                    href={websiteUrl}
                                    target="_blank"
                                    rel="noreferrer"
                                    className="font-semibold text-foreground hover:text-primary"
                                >
                                    Visit website
                                </a>
                            )}

                            <Link
                                to="/products"
                                className="font-semibold text-foreground hover:text-primary"
                            >
                                Browse products
                            </Link>
                        </div>
                    </div>
                </div>
            </footer>
        </div>
    );
}
