import {
    ArrowLeft,
    ArrowRight,
    Search,
    ShoppingBag,
    Sparkles,
} from 'lucide-react';

import {
    type FormEvent,
    useState,
} from 'react';

import {
    Link,
    useNavigate,
} from 'react-router-dom';

import {
    useCategories,
} from '@/modules/catalog/hooks/useCategories';

import {
    useProducts,
} from '@/modules/catalog/hooks/useProducts';

import ProductCard from '@/modules/catalog/components/ProductCard';

function StoreHeader() {
    const navigate = useNavigate();
    const [query, setQuery] = useState('');

    const submitSearch = (
        event: FormEvent<HTMLFormElement>,
    ) => {
        event.preventDefault();

        const value = query.trim();

        if (!value) {
            navigate('/products');
            return;
        }

        navigate(
            `/products?search=${encodeURIComponent(value)}`,
        );
    };

    return (
        <header className="sticky top-0 z-40 border-b bg-background/95 backdrop-blur">
            <div className="mx-auto flex max-w-7xl items-center gap-3 px-4 py-3 sm:px-6">
                <Link
                    to="/"
                    className="flex shrink-0 items-center gap-2"
                >
                    <div className="flex size-10 items-center justify-center rounded-xl bg-primary text-primary-foreground">
                        <ShoppingBag className="size-5" />
                    </div>

                    <div className="hidden sm:block">
                        <div className="text-lg font-black tracking-tight">
                            NexaECommerce
                        </div>

                        <div className="text-[11px] text-muted-foreground">
                            Online Store
                        </div>
                    </div>
                </Link>

                <form
                    onSubmit={submitSearch}
                    className="mx-auto flex min-w-0 flex-1"
                >
                    <div className="flex w-full items-center rounded-2xl border bg-muted/40 px-3 transition-colors focus-within:border-primary">
                        <Search className="size-5 shrink-0 text-muted-foreground" />

                        <input
                            value={query}
                            onChange={(event) =>
                                setQuery(event.target.value)
                            }
                            placeholder="Search products..."
                            className="h-11 w-full bg-transparent px-3 text-sm outline-none"
                        />

                        <button
                            type="submit"
                            className="hidden rounded-xl bg-primary px-4 py-2 text-sm font-bold text-primary-foreground sm:block"
                        >
                            Search
                        </button>
                    </div>
                </form>

                <Link
                    to="/products"
                    className="hidden rounded-xl border px-4 py-2 text-sm font-semibold hover:bg-muted lg:block"
                >
                    Products
                </Link>

                <Link
                    to="/cart"
                    className="flex size-11 shrink-0 items-center justify-center rounded-xl border hover:bg-muted"
                    aria-label="Shopping cart"
                >
                    <ShoppingBag className="size-5" />
                </Link>

                <Link
                    to="/login"
                    className="hidden rounded-xl bg-primary px-4 py-2 text-sm font-bold text-primary-foreground md:block"
                >
                    Sign in
                </Link>
            </div>
        </header>
    );
}

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

function Hero() {
    return (
        <section className="overflow-hidden rounded-[2rem] border bg-gradient-to-br from-primary/10 via-background to-muted p-6 sm:p-10">
            <div className="grid items-center gap-8 lg:grid-cols-[1.2fr_.8fr]">
                <div className="max-w-2xl">
                    <div className="mb-4 inline-flex items-center gap-2 rounded-full border bg-background/80 px-3 py-1.5 text-xs font-bold">
                        <Sparkles className="size-4 text-primary" />
                        Welcome to NexaECommerce
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
                        <div className="absolute inset-0 rounded-[3rem] bg-primary/10 rotate-6" />
                        <div className="absolute inset-4 rounded-[2.5rem] border bg-background shadow-2xl" />

                        <div className="absolute inset-0 flex items-center justify-center">
                            <ShoppingBag className="size-28 text-primary" />
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
                {categories.slice(0, 6).map((category) => (
                    <Link
                        key={category.id}
                        to={`/products?categoryId=${category.id}`}
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

                            {typeof category.productCount === 'number' && (
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
            {Array.from({ length: 8 }).map((_, index) => (
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

    return (
        <div className="min-h-screen bg-background">
            <StoreHeader />

            <main className="mx-auto max-w-7xl space-y-12 px-4 py-6 sm:px-6 sm:py-8">
                <Hero />

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
                            {featured.items.map((product) => (
                                <ProductCard
                                    key={product.id}
                                    product={product}
                                />
                            ))}
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
                            {latest.items.map((product) => (
                                <ProductCard
                                    key={product.id}
                                    product={product}
                                />
                            ))}
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
                    <div className="flex flex-col gap-2 sm:flex-row sm:items-center sm:justify-between">
                        <span>
                            © {new Date().getFullYear()} NexaECommerce
                        </span>

                        <Link
                            to="/products"
                            className="font-semibold text-foreground"
                        >
                            Browse products
                        </Link>
                    </div>
                </div>
            </footer>
        </div>
    );
}