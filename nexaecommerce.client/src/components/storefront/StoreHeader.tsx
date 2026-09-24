import {
    type FormEvent,
    useMemo,
    useState,
} from 'react';

import {
    Link,
    useNavigate,
} from 'react-router-dom';

import {
    useQuery,
} from '@tanstack/react-query';

import {
    Search,
    ShoppingBag,
    UserRound,
} from 'lucide-react';

import { appearanceApi } from '@/lib/api/appearance';

import {
    useAuth,
} from '@/hooks/use-auth';

import {
    useCart,
} from '@/modules/cart/hooks/useCart';

import {
    LanguageToggle,
} from '@/components/language-toggle';

import {
    useTranslation,
} from 'react-i18next';

const DEFAULT_STORE_NAME =
    'NexaECommerce';

export default function StoreHeader() {
    const navigate =
        useNavigate();

    const {
        i18n,
    } = useTranslation();

    const isFa =
        i18n.language
            ?.toLowerCase()
            .startsWith('fa');

    const [
        query,
        setQuery,
    ] = useState('');

    const {
        user,
        isAuthenticated,
        isLoading:
        authLoading,
    } = useAuth();

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
        data: cart,
        isLoading:
        cartLoading,
    } = useCart();

    const storeName =
        appearance?.storeName?.trim() ||
        DEFAULT_STORE_NAME;

    const logoUrl =
        appearance?.logoUrl?.trim() ||
        null;

    const userName =
        user?.displayName?.trim() ||
        user?.email?.trim() ||
        (
            isFa
                ? 'حساب کاربری'
                : 'My account'
        );

    const userInitial =
        userName
            .charAt(0)
            .toUpperCase() ||
        'U';

    const cartItemCount =
        useMemo(
            () =>
                cart?.items.reduce(
                    (
                        total,
                        item,
                    ) =>
                        total +
                        item.quantity,
                    0,
                ) ?? 0,
            [
                cart,
            ],
        );

    const hasCartItems =
        cartItemCount > 0;

    const submitSearch = (
        event: FormEvent<HTMLFormElement>,
    ) => {
        event.preventDefault();

        const value =
            query.trim();

        if (!value) {
            navigate(
                '/products',
            );

            return;
        }

        navigate(
            `/products?search=${encodeURIComponent(value)}`,
        );
    };

    const searchPlaceholder =
        isFa
            ? 'جستجوی محصولات...'
            : 'Search products...';

    const searchLabel =
        isFa
            ? 'جستجوی محصولات'
            : 'Search products';

    const searchButton =
        isFa
            ? 'جستجو'
            : 'Search';

    const productsLabel =
        isFa
            ? 'محصولات'
            : 'Products';

    const onlineStoreLabel =
        isFa
            ? 'فروشگاه آنلاین'
            : 'Online Store';

    const profileLabel =
        isFa
            ? 'پروفایل من'
            : 'My profile';

    const signInLabel =
        isFa
            ? 'ورود'
            : 'Sign in';

    const cartLabel =
        hasCartItems
            ? isFa
                ? `سبد خرید با ${cartItemCount} کالا`
                : `Shopping cart with ${cartItemCount} items`
            : isFa
                ? 'سبد خرید خالی است'
                : 'Shopping cart is empty';

    const cartTitle =
        hasCartItems
            ? isFa
                ? `سبد خرید (${cartItemCount})`
                : `Shopping cart (${cartItemCount})`
            : isFa
                ? 'سبد خرید خالی است'
                : 'Shopping cart is empty';

    return (
        <header className="sticky top-0 z-40 border-b bg-background/95 backdrop-blur">
            <div
                className="mx-auto flex max-w-7xl items-center gap-3 px-4 py-3 sm:px-6"
                dir={
                    isFa
                        ? 'rtl'
                        : 'ltr'
                }
            >
                <Link
                    to="/"
                    className="flex shrink-0 items-center gap-2"
                    aria-label={
                        storeName
                    }
                >
                    {logoUrl ? (
                        <img
                            src={
                                logoUrl
                            }
                            alt=""
                            className="size-10 shrink-0 rounded-xl object-contain"
                        />
                    ) : (
                        <div className="flex size-10 items-center justify-center rounded-xl bg-primary text-primary-foreground">
                            <ShoppingBag className="size-5" />
                        </div>
                    )}

                    <div className="hidden sm:block">
                        <div className="max-w-52 truncate text-lg font-black tracking-tight">
                            {
                                storeName
                            }
                        </div>

                        <div className="text-[11px] text-muted-foreground">
                            {
                                onlineStoreLabel
                            }
                        </div>
                    </div>
                </Link>

                <form
                    onSubmit={
                        submitSearch
                    }
                    className="mx-auto flex min-w-0 flex-1"
                >
                    <div className="flex w-full items-center rounded-2xl border bg-muted/40 px-3 transition-colors focus-within:border-primary">
                        <Search className="size-5 shrink-0 text-muted-foreground" />

                        <input
                            value={
                                query
                            }
                            onChange={event =>
                                setQuery(
                                    event
                                        .target
                                        .value,
                                )
                            }
                            placeholder={
                                searchPlaceholder
                            }
                            className="h-11 w-full bg-transparent px-3 text-sm outline-none"
                            aria-label={
                                searchLabel
                            }
                        />

                        <button
                            type="submit"
                            className="hidden rounded-xl bg-primary px-4 py-2 text-sm font-bold text-primary-foreground transition-opacity hover:opacity-90 sm:block"
                        >
                            {
                                searchButton
                            }
                        </button>
                    </div>
                </form>

                <Link
                    to="/products"
                    className="hidden rounded-xl border px-4 py-2 text-sm font-semibold transition-colors hover:bg-muted lg:block"
                >
                    {
                        productsLabel
                    }
                </Link>

                <Link
                    to="/cart"
                    className={`relative flex size-11 shrink-0 items-center justify-center rounded-xl border transition-colors hover:bg-muted ${hasCartItems
                            ? 'border-primary/50 bg-primary/5'
                            : 'text-muted-foreground'
                        }`}
                    aria-label={
                        cartLabel
                    }
                    title={
                        cartTitle
                    }
                >
                    <ShoppingBag
                        className={`size-5 ${hasCartItems
                                ? 'text-primary'
                                : ''
                            }`}
                    />

                    {hasCartItems && (
                        <span
                            className="absolute -end-1 -top-1 flex min-w-5 items-center justify-center rounded-full bg-primary px-1.5 py-0.5 text-[10px] font-bold leading-none text-primary-foreground ring-2 ring-background"
                            aria-hidden="true"
                        >
                            {cartItemCount >
                                99
                                ? '99+'
                                : cartItemCount}
                        </span>
                    )}

                    {cartLoading && (
                        <span
                            className="absolute bottom-1 start-1 size-1.5 animate-pulse rounded-full bg-muted-foreground"
                            aria-hidden="true"
                        />
                    )}
                </Link>

                <LanguageToggle />

                {isAuthenticated ? (
                    <Link
                        to="/profile"
                        className="flex min-w-0 shrink-0 items-center gap-2 rounded-xl border px-2 py-2 transition-colors hover:bg-muted md:px-3"
                        aria-label={
                            isFa
                                ? `باز کردن پروفایل ${userName}`
                                : `Open profile for ${userName}`
                        }
                        title={
                            profileLabel
                        }
                    >
                        {user?.avatarUrl?.trim() ? (
                            <img
                                src={
                                    user.avatarUrl
                                }
                                alt=""
                                className="size-9 shrink-0 rounded-full object-cover"
                            />
                        ) : (
                            <div className="flex size-9 shrink-0 items-center justify-center rounded-full bg-primary text-sm font-bold text-primary-foreground">
                                {
                                    userInitial
                                }
                            </div>
                        )}

                        <div className="hidden min-w-0 max-w-40 md:block">
                            <div className="truncate text-sm font-bold">
                                {
                                    userName
                                }
                            </div>

                            <div className="text-[11px] text-muted-foreground">
                                {
                                    profileLabel
                                }
                            </div>
                        </div>

                        <UserRound className="hidden size-4 shrink-0 text-muted-foreground lg:block" />
                    </Link>
                ) : authLoading ? (
                    <div
                        className="hidden h-11 w-24 animate-pulse rounded-xl border bg-muted md:block"
                        aria-hidden="true"
                    />
                ) : (
                    <Link
                        to="/login"
                        className="hidden rounded-xl bg-primary px-4 py-2 text-sm font-bold text-primary-foreground transition-opacity hover:opacity-90 md:block"
                    >
                        {
                            signInLabel
                        }
                    </Link>
                )}
            </div>
        </header>
    );
}