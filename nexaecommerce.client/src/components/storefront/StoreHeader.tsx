import { useMemo, useState, type FormEvent } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';

import {
    Search,
    ShoppingBag,
    UserRound,
    Menu,
    X,
    ChevronDown,
    Home,
    Package,
} from 'lucide-react';

import { ModeToggle } from '@/components/mode-toggle';
import { LanguageToggle } from '@/components/language-toggle';

import { appearanceApi } from '@/lib/api/appearance';
import { useAuth } from '@/hooks/use-auth';
import { useCart } from '@/modules/cart/hooks/useCart';
import { useCategories } from '@/modules/catalog/hooks/useCategories';

import type { Category } from '@/modules/catalog/api/categories';

import { useTranslation } from 'react-i18next';

const DEFAULT_STORE_NAME = 'NexaECommerce';

function getCategoryChildren(
    category: Category,
    categories: Category[],
): Category[] {
    if (category.subCategories?.length) {
        return category.subCategories;
    }

    return categories.filter(
        (item) => item.parentCategoryId === category.id,
    );
}

export default function StoreHeader() {
    const navigate = useNavigate();

    const { i18n, t } = useTranslation();

    const isFa =
        i18n.language?.toLowerCase().startsWith('fa') ?? false;

    const [query, setQuery] = useState('');
    const [mobileMenuOpen, setMobileMenuOpen] = useState(false);

    const {
        user,
        isAuthenticated,
        isLoading: authLoading,
    } = useAuth();

    const { data: appearance } = useQuery({
        queryKey: ['appearance'],
        queryFn: appearanceApi.get,
        staleTime: 5 * 60_000,
        retry: 1,
    });

    const { data: cart, isLoading: cartLoading } = useCart();

    const { data: categoryData = [] } = useCategories();

    const categories = Array.isArray(categoryData)
        ? categoryData
        : [];

    const rootCategories = useMemo(
        () =>
            categories
                .filter((category) => !category.parentCategoryId)
                .sort((a, b) => a.displayOrder - b.displayOrder)
                .slice(0, 7),
        [categories],
    );

    const storeName =
        appearance?.storeName?.trim() || DEFAULT_STORE_NAME;

    const logoUrl = appearance?.logoUrl?.trim() || null;

    const userName =
        user?.displayName?.trim() ||
        user?.email?.trim() ||
        (isFa ? 'حساب کاربری' : 'My account');

    const cartItemCount = useMemo(
        () =>
            cart?.items.reduce(
                (total, item) => total + item.quantity,
                0,
            ) ?? 0,
        [cart],
    );

    const hasCartItems = cartItemCount > 0;

    const submitSearch = (event: FormEvent<HTMLFormElement>) => {
        event.preventDefault();

        const value = query.trim();

        if (!value) {
            navigate('/products');
            return;
        }

        navigate(`/products?search=${encodeURIComponent(value)}`);

        setMobileMenuOpen(false);
    };

    const text = {
        home: t('storefront.brand.home', {
            defaultValue: 'خانه',
        }),

        products: t('storefront.brand.products', {
            defaultValue: 'همه محصولات',
        }),

        search: t('storefront.header.searchProducts', {
            defaultValue: 'جست‌وجوی محصولات، برندها و دسته‌بندی‌ها...',
        }),

        searchButton: t('storefront.header.searchButton', {
            defaultValue: 'جست‌وجو',
        }),

        login: t('storefront.brand.signIn', {
            defaultValue: 'ورود / ثبت‌نام',
        }),

        profile: t('storefront.brand.myProfile', {
            defaultValue: 'حساب کاربری',
        }),

        cart: t('storefront.header.cartEmpty', {
            defaultValue: 'سبد خرید',
        }),

        cartWithItems: t('storefront.header.cartWithItems', {
            count: cartItemCount,
            defaultValue: `سبد خرید (${cartItemCount})`,
        }),

        categories: t('storefront.header.categories', {
            defaultValue: 'دسته‌بندی کالاها',
        }),
    };

    const isCartEmpty = cartItemCount === 0;

    return (
        <header
            className="sticky top-0 z-40 border-b border-slate-200 bg-white/95 shadow-sm backdrop-blur-xl"
            dir={isFa ? 'rtl' : 'ltr'}
        >
            {/* Top announcement */}
            <div className="bg-slate-900 text-white">
                <div className="mx-auto flex max-w-7xl items-center justify-center px-4 py-2 text-center text-[11px] font-medium sm:text-xs">
                    <span>
                        تجربه‌ای بهتر برای انتخاب و خرید آنلاین
                    </span>
                </div>
            </div>

            {/* Main header */}
            <div className="mx-auto max-w-7xl px-4 sm:px-6">

                <div className="flex min-h-[76px] items-center gap-3 lg:gap-6">

                    {/* Mobile menu */}
                    <button
                        type="button"
                        className="flex size-10 shrink-0 items-center justify-center rounded-xl border border-slate-200 text-slate-700 transition hover:bg-slate-50 lg:hidden"
                        onClick={() => setMobileMenuOpen((value) => !value)}
                        aria-label={
                            mobileMenuOpen
                                ? 'بستن منو'
                                : 'بازکردن منو'
                        }
                        aria-expanded={mobileMenuOpen}
                    >
                        {mobileMenuOpen ? (
                            <X className="size-5" />
                        ) : (
                            <Menu className="size-5" />
                        )}
                    </button>

                    {/* Logo */}
                    <Link
                        to="/"
                        className="flex shrink-0 items-center gap-3"
                        aria-label={storeName}
                    >
                        {logoUrl ? (
                            <img
                                src={logoUrl}
                                alt={storeName}
                                className="size-11 rounded-xl object-contain"
                            />
                        ) : (
                            <div className="flex size-11 items-center justify-center rounded-2xl bg-primary text-primary-foreground shadow-sm">
                                <ShoppingBag className="size-6" />
                            </div>
                        )}

                        <div className="hidden min-w-0 sm:block">
                            <div className="max-w-48 truncate text-lg font-black tracking-tight text-slate-900">
                                {storeName}
                            </div>

                            <div className="text-[10px] font-medium text-slate-500">
                                فروشگاه اینترنتی
                            </div>
                        </div>
                    </Link>

                    {/* Search */}
                    <form
                        onSubmit={submitSearch}
                        className="mx-auto hidden min-w-0 flex-1 md:flex"
                    >
                        <div className="flex h-12 w-full items-center rounded-2xl border border-slate-200 bg-slate-100/80 px-3 transition-all focus-within:border-primary focus-within:bg-white focus-within:ring-4 focus-within:ring-primary/10">
                            <Search className="size-5 shrink-0 text-slate-400" />

                            <input
                                value={query}
                                onChange={(event) =>
                                    setQuery(event.target.value)
                                }
                                placeholder={text.search}
                                aria-label={text.search}
                                className="h-full min-w-0 flex-1 bg-transparent px-3 text-sm text-slate-800 outline-none placeholder:text-slate-400"
                            />

                            <button
                                type="submit"
                                className="h-9 rounded-xl bg-primary px-5 text-sm font-bold text-primary-foreground transition hover:opacity-90"
                            >
                                {text.searchButton}
                            </button>
                        </div>
                    </form>

                    {/* Actions */}
                    <div className="ms-auto flex shrink-0 items-center gap-2">

                        <LanguageToggle />

                        <ModeToggle />

                        {/* Account */}
                        {isAuthenticated ? (
                            <Link
                                to="/profile"
                                className="hidden h-11 items-center gap-2 rounded-xl border border-slate-200 px-3 transition hover:border-primary/40 hover:bg-primary/5 sm:flex"
                            >
                                {user?.avatarUrl?.trim() ? (
                                    <img
                                        src={user.avatarUrl}
                                        alt=""
                                        className="size-8 rounded-full object-cover"
                                    />
                                ) : (
                                    <span className="flex size-8 items-center justify-center rounded-full bg-primary/10 text-primary">
                                        <UserRound className="size-4" />
                                    </span>
                                )}

                                <span className="max-w-28 truncate text-xs font-bold text-slate-700">
                                    {userName}
                                </span>
                            </Link>
                        ) : authLoading ? (
                            <div className="hidden h-11 w-28 animate-pulse rounded-xl bg-slate-100 sm:block" />
                        ) : (
                            <Link
                                to="/login"
                                className="hidden h-11 items-center gap-2 rounded-xl border border-primary/30 bg-primary/5 px-4 text-sm font-bold text-primary transition hover:bg-primary/10 sm:flex"
                            >
                                <UserRound className="size-4" />
                                {text.login}
                            </Link>
                        )}

                        {/* Cart */}
                        <Link
                            to="/cart"
                            className={`relative flex size-11 shrink-0 items-center justify-center rounded-xl border transition ${hasCartItems
                                    ? 'border-primary/30 bg-primary/5 text-primary'
                                    : 'border-slate-200 text-slate-600 hover:bg-slate-50'
                                }`}
                            aria-label={
                                isCartEmpty
                                    ? text.cart
                                    : text.cartWithItems
                            }
                            title={
                                isCartEmpty
                                    ? text.cart
                                    : text.cartWithItems
                            }
                        >
                            <ShoppingBag className="size-5" />

                            {hasCartItems && (
                                <span className="absolute -end-1 -top-1 flex min-w-5 items-center justify-center rounded-full bg-primary px-1.5 py-0.5 text-[10px] font-bold text-primary-foreground ring-2 ring-white">
                                    {cartItemCount > 99
                                        ? '99+'
                                        : cartItemCount}
                                </span>
                            )}

                            {cartLoading && (
                                <span className="absolute bottom-1 start-1 size-1.5 animate-pulse rounded-full bg-slate-400" />
                            )}
                        </Link>
                    </div>
                </div>

                {/* Mobile search */}
                <form
                    onSubmit={submitSearch}
                    className="pb-3 md:hidden"
                >
                    <div className="flex h-11 items-center rounded-xl border border-slate-200 bg-slate-100/80 px-3 focus-within:border-primary focus-within:bg-white">
                        <Search className="size-5 shrink-0 text-slate-400" />

                        <input
                            value={query}
                            onChange={(event) =>
                                setQuery(event.target.value)
                            }
                            placeholder={text.search}
                            aria-label={text.search}
                            className="h-full min-w-0 flex-1 bg-transparent px-3 text-xs outline-none"
                        />

                        <button
                            type="submit"
                            aria-label={text.searchButton}
                        >
                            <Search className="size-5 text-primary" />
                        </button>
                    </div>
                </form>
            </div>

            {/* Desktop navigation */}
            <nav className="hidden border-t border-slate-100 lg:block">
                <div className="mx-auto flex h-12 max-w-7xl items-center gap-7 px-6">

                    <Link
                        to="/"
                        className="flex items-center gap-2 text-sm font-bold text-slate-700 transition hover:text-primary"
                    >
                        <Home className="size-4" />
                        {text.home}
                    </Link>

                    <Link
                        to="/products"
                        className="flex items-center gap-2 text-sm font-bold text-slate-700 transition hover:text-primary"
                    >
                        <Package className="size-4" />
                        {text.products}
                    </Link>

                    <div className="h-5 w-px bg-slate-200" />

                    {rootCategories.map((category) => {
                        const children = getCategoryChildren(
                            category,
                            categories,
                        );

                        return (
                            <div
                                key={category.id}
                                className="group relative flex h-12 items-center"
                            >
                                <Link
                                    to={`/products?categoryId=${encodeURIComponent(category.id)}`}
                                    className="flex items-center gap-1.5 text-sm font-medium text-slate-600 transition hover:text-primary"
                                >
                                    {category.name}

                                    {children.length > 0 && (
                                        <ChevronDown className="size-3.5 transition-transform group-hover:rotate-180" />
                                    )}
                                </Link>

                                {children.length > 0 && (
                                    <div className="invisible absolute end-0 top-full z-50 w-64 translate-y-2 rounded-2xl border border-slate-200 bg-white p-3 opacity-0 shadow-xl transition-all group-hover:visible group-hover:translate-y-0 group-hover:opacity-100 group-focus-within:visible group-focus-within:translate-y-0 group-focus-within:opacity-100">
                                        <Link
                                            to={`/products?categoryId=${encodeURIComponent(category.id)}`}
                                            className="mb-2 block rounded-xl bg-slate-50 px-3 py-2.5 text-sm font-bold text-primary hover:bg-primary/5"
                                        >
                                            مشاهده همه {category.name}
                                        </Link>

                                        <div className="space-y-1">
                                            {children.map((child) => (
                                                <Link
                                                    key={child.id}
                                                    to={`/products?categoryId=${encodeURIComponent(child.id)}`}
                                                    className="block rounded-lg px-3 py-2 text-sm text-slate-600 transition hover:bg-slate-50 hover:text-primary"
                                                >
                                                    {child.name}
                                                </Link>
                                            ))}
                                        </div>
                                    </div>
                                )}
                            </div>
                        );
                    })}
                </div>
            </nav>

            {/* Mobile navigation */}
            {mobileMenuOpen && (
                <div className="border-t border-slate-200 bg-white px-4 py-4 shadow-lg lg:hidden">
                    <div className="space-y-1">

                        <Link
                            to="/"
                            onClick={() => setMobileMenuOpen(false)}
                            className="flex items-center gap-3 rounded-xl px-3 py-3 text-sm font-bold text-slate-700 hover:bg-slate-50"
                        >
                            <Home className="size-5" />
                            {text.home}
                        </Link>

                        <Link
                            to="/products"
                            onClick={() => setMobileMenuOpen(false)}
                            className="flex items-center gap-3 rounded-xl px-3 py-3 text-sm font-bold text-slate-700 hover:bg-slate-50"
                        >
                            <Package className="size-5" />
                            {text.products}
                        </Link>

                        <div className="my-3 border-t" />

                        <div className="px-3 pb-2 text-xs font-bold text-slate-400">
                            {text.categories}
                        </div>

                        {rootCategories.map((category) => (
                            <div key={category.id}>
                                <Link
                                    to={`/products?categoryId=${encodeURIComponent(category.id)}`}
                                    onClick={() => setMobileMenuOpen(false)}
                                    className="block rounded-xl px-3 py-3 text-sm font-semibold text-slate-700 hover:bg-slate-50"
                                >
                                    {category.name}
                                </Link>

                                {getCategoryChildren(
                                    category,
                                    categories,
                                ).map((child) => (
                                    <Link
                                        key={child.id}
                                        to={`/products?categoryId=${encodeURIComponent(child.id)}`}
                                        onClick={() => setMobileMenuOpen(false)}
                                        className="block rounded-xl px-6 py-2 text-xs text-slate-500 hover:bg-slate-50 hover:text-primary"
                                    >
                                        {child.name}
                                    </Link>
                                ))}
                            </div>
                        ))}

                        {!isAuthenticated && (
                            <Link
                                to="/login"
                                onClick={() => setMobileMenuOpen(false)}
                                className="mt-3 flex items-center justify-center rounded-xl bg-primary px-4 py-3 text-sm font-bold text-primary-foreground"
                            >
                                {text.login}
                            </Link>
                        )}

                        {isAuthenticated && (
                            <Link
                                to="/profile"
                                onClick={() => setMobileMenuOpen(false)}
                                className="mt-3 flex items-center gap-2 rounded-xl border px-4 py-3 text-sm font-bold"
                            >
                                <UserRound className="size-4" />
                                {text.profile}
                            </Link>
                        )}
                    </div>
                </div>
            )}
        </header>
    );
}