import {
    type FormEvent,
    useMemo,
    useRef,
    useState,
} from 'react';

import {
    ArrowLeft,
    ArrowRight,
    CreditCard,
    MapPin,
    Package,
    Truck,
} from 'lucide-react';

import {
    Link,
    useNavigate,
} from 'react-router-dom';

import { useTranslation } from 'react-i18next';

import { useCart } from '@/modules/cart/hooks/useCart';
import { useCheckout } from '@/modules/orders/hooks/useCheckout';
import { useShippingMethods } from '@/modules/orders/hooks/useShippingMethods';

import type {
    CheckoutRequest,
} from '@/modules/orders/types';

function formatMoney(
    amount: number,
    currency: string,
) {
    return (
        new Intl.NumberFormat(
            undefined,
            {
                maximumFractionDigits: 0,
            },
        ).format(amount) +
        ` ${currency}`
    );
}

function SkeletonLine({
    className = '',
}: {
    className?: string;
}) {
    return (
        <div
            className={`animate-pulse rounded-lg bg-muted ${className}`}
        />
    );
}

export default function CheckoutPage() {
    const { t, i18n } =
        useTranslation();

    
const navigate =
    useNavigate();

const isFa =
    i18n.language
        ?.toLowerCase()
        .startsWith('fa');

const cartQuery =
    useCart();

const shippingMethodsQuery =
    useShippingMethods();

const checkout =
    useCheckout();

const checkoutKeyRef =
    useRef<string | null>(null);

const [fullName, setFullName] =
    useState('');

const [phone, setPhone] =
    useState('');

const [address, setAddress] =
    useState('');

const [city, setCity] =
    useState('');

const [postalCode, setPostalCode] =
    useState('');

const [shippingMethodId, setShippingMethodId] =
    useState('');

const [validationError, setValidationError] =
    useState<string | null>(null);

const getText = (
    key: string,
    fallback: string,
) =>
    t(
        key,
        {
            defaultValue:
                fallback,
        },
    );

const selectedShippingMethod =
    useMemo(
        () =>
            shippingMethodsQuery.data?.find(
                method =>
                    method.id ===
                    shippingMethodId,
            ) ?? null,
        [
            shippingMethodsQuery.data,
            shippingMethodId,
        ],
    );

const subtotal =
    cartQuery.data?.subtotal ??
    0;

const shippingAmount =
    selectedShippingMethod?.price ??
    0;

const estimatedTotal =
    subtotal +
    shippingAmount;

const handleSubmit = (
    event: FormEvent<HTMLFormElement>,
) => {
    event.preventDefault();

    setValidationError(null);

    const cart =
        cartQuery.data;

    if (
        !cart ||
        cart.items.length === 0
    ) {
        setValidationError(
            getText(
                'checkout.emptyCart',
                'Your shopping cart is empty.',
            ),
        );

        return;
    }

    if (!fullName.trim()) {
        setValidationError(
            getText(
                'checkout.validation.fullName',
                'Full name is required.',
            ),
        );

        return;
    }

    if (!phone.trim()) {
        setValidationError(
            getText(
                'checkout.validation.phone',
                'Phone number is required.',
            ),
        );

        return;
    }

    if (!address.trim()) {
        setValidationError(
            getText(
                'checkout.validation.address',
                'Address is required.',
            ),
        );

        return;
    }

    if (!city.trim()) {
        setValidationError(
            getText(
                'checkout.validation.city',
                'City is required.',
            ),
        );

        return;
    }

    if (!shippingMethodId) {
        setValidationError(
            getText(
                'checkout.validation.shipping',
                'Please select a shipping method.',
            ),
        );

        return;
    }

    if (!checkoutKeyRef.current) {
        checkoutKeyRef.current =
            crypto.randomUUID();
    }

    const request: CheckoutRequest = {
        items:
            cart.items.map(
                item => ({
                    productVariantId:
                        item.productVariantId,
                    quantity:
                        item.quantity,
                }),
            ),

        shippingFullName:
            fullName.trim(),

        shippingPhone:
            phone.trim(),

        shippingAddress:
            address.trim(),

        shippingCity:
            city.trim(),

        shippingPostalCode:
            postalCode.trim() ||
            null,

        shippingMethodId,
    };

    checkout.mutate(
        {
            request,
            idempotencyKey:
                checkoutKeyRef.current,
        },
        {
            onSuccess: order => {
                navigate(
                    `/ orders / ${ order.id } `,
                    {
                        replace: true,
                    },
                );
            },
        },
    );
};

if (cartQuery.isLoading) {
    return (
        <div className="mx-auto max-w-7xl space-y-6 p-4 md:p-6">
            <SkeletonLine className="h-8 w-40" />

            <div className="grid gap-6 lg:grid-cols-[1fr_380px]">
                <div className="space-y-4 rounded-2xl border p-6">
                    <SkeletonLine className="h-10 w-full" />
                    <SkeletonLine className="h-10 w-full" />
                    <SkeletonLine className="h-28 w-full" />
                    <SkeletonLine className="h-10 w-full" />
                    <SkeletonLine className="h-12 w-full" />
                </div>

                <div className="space-y-4 rounded-2xl border p-6">
                    <SkeletonLine className="h-6 w-32" />
                    <SkeletonLine className="h-5 w-full" />
                    <SkeletonLine className="h-5 w-full" />
                    <SkeletonLine className="h-8 w-full" />
                </div>
            </div>
        </div>
    );
}

if (cartQuery.isError) {
    return (
        <div
            dir={isFa ? 'rtl' : 'ltr'}
            className="mx-auto max-w-3xl p-6"
        >
            <div className="rounded-2xl border p-10 text-center">
                <h1 className="text-2xl font-semibold">
                    {getText(
                        'checkout.error.title',
                        'Unable to load checkout',
                    )}
                </h1>

                <p className="mt-2 text-muted-foreground">
                    {getText(
                        'checkout.error.description',
                        'We could not load your shopping cart. Please try again.',
                    )}
                </p>

                <button
                    type="button"
                    onClick={() =>
                        cartQuery.refetch()
                    }
                    className="mt-6 rounded-xl border px-5 py-3 font-medium"
                >
                    {getText(
                        'common.retry',
                        'Retry',
                    )}
                </button>
            </div>
        </div>
    );
}

const cart =
    cartQuery.data;

if (
    !cart ||
    cart.items.length === 0
) {
    return (
        <div
            dir={isFa ? 'rtl' : 'ltr'}
            className="mx-auto max-w-3xl p-6"
        >
            <div className="rounded-2xl border p-10 text-center">
                <Package className="mx-auto size-12 text-muted-foreground" />

                <h1 className="mt-4 text-2xl font-semibold">
                    {getText(
                        'checkout.empty.title',
                        'Your cart is empty',
                    )}
                </h1>

                <p className="mt-2 text-muted-foreground">
                    {getText(
                        'checkout.empty.description',
                        'Add some products before continuing to checkout.',
                    )}
                </p>

                <Link
                    to="/products"
                    className="mt-6 inline-flex items-center gap-2 rounded-xl border px-5 py-3 font-medium"
                >
                    {isFa ? (
                        <ArrowRight className="size-4" />
                    ) : (
                        <ArrowLeft className="size-4" />
                    )}

                    {getText(
                        'checkout.continueShopping',
                        'Continue shopping',
                    )}
                </Link>
            </div>
        </div>
    );
}

return (
    <div
        dir={isFa ? 'rtl' : 'ltr'}
        className="mx-auto max-w-7xl space-y-6 p-4 md:p-6"
    >
        <header>
            <div className="flex items-center gap-3">
                <div className="rounded-xl border p-2">
                    <CreditCard className="size-5" />
                </div>

                <div>
                    <h1 className="text-3xl font-bold tracking-tight">
                        {getText(
                            'checkout.title',
                            'Checkout',
                        )}
                    </h1>

                    <p className="mt-1 text-muted-foreground">
                        {getText(
                            'checkout.subtitle',
                            'Complete your delivery details and choose a shipping method.',
                        )}
                    </p>
                </div>
            </div>
        </header>

        {validationError && (
            <div
                role="alert"
                className="rounded-xl border border-destructive/30 bg-destructive/5 px-4 py-3 text-sm"
            >
                {validationError}
            </div>
        )}

        {checkout.isError && (
            <div
                role="alert"
                className="rounded-xl border border-destructive/30 bg-destructive/5 px-4 py-3"
            >
                <p className="font-medium">
                    {getText(
                        'checkout.submitError',
                        'We could not complete your checkout.',
                    )}
                </p>

                <p className="mt-1 text-sm text-muted-foreground">
                    {getText(
                        'checkout.submitErrorDescription',
                        'Your order was not confirmed. Please review your details and try again.',
                    )}
                </p>

                <button
                    type="button"
                    onClick={() => {
                        const form =
                            document.getElementById(
                                'checkout-form',
                            ) as HTMLFormElement | null;

                        form?.requestSubmit();
                    }}
                    className="mt-3 rounded-lg border px-4 py-2 text-sm font-medium"
                >
                    {getText(
                        'common.retry',
                        'Retry',
                    )}
                </button>
            </div>
        )}

        <form
            id="checkout-form"
            onSubmit={handleSubmit}
            className="grid gap-6 lg:grid-cols-[1fr_380px]"
        >
            <section className="space-y-6">
                <div className="rounded-2xl border p-5 md:p-6">
                    <div className="flex items-center gap-3">
                        <MapPin className="size-5" />

                        <div>
                            <h2 className="font-semibold">
                                {getText(
                                    'checkout.shippingAddress',
                                    'Shipping address',
                                )}
                            </h2>

                            <p className="text-sm text-muted-foreground">
                                {getText(
                                    'checkout.shippingAddressHint',
                                    'Where should we deliver your order?',
                                )}
                            </p>
                        </div>
                    </div>

                    <div className="mt-6 grid gap-4 md:grid-cols-2">
                        <label className="grid gap-2">
                            <span className="text-sm font-medium">
                                {getText(
                                    'checkout.fullName',
                                    'Full name',
                                )}
                            </span>

                            <input
                                value={fullName}
                                onChange={event =>
                                    setFullName(
                                        event.target.value,
                                    )
                                }
                                autoComplete="name"
                                required
                                className="h-11 rounded-xl border bg-background px-3 outline-none focus:ring-2 focus:ring-ring"
                            />
                        </label>

                        <label className="grid gap-2">
                            <span className="text-sm font-medium">
                                {getText(
                                    'checkout.phone',
                                    'Phone number',
                                )}
                            </span>

                            <input
                                value={phone}
                                onChange={event =>
                                    setPhone(
                                        event.target.value,
                                    )
                                }
                                autoComplete="tel"
                                inputMode="tel"
                                required
                                className="h-11 rounded-xl border bg-background px-3 outline-none focus:ring-2 focus:ring-ring"
                            />
                        </label>

                        <label className="grid gap-2 md:col-span-2">
                            <span className="text-sm font-medium">
                                {getText(
                                    'checkout.address',
                                    'Address',
                                )}
                            </span>

                            <textarea
                                value={address}
                                onChange={event =>
                                    setAddress(
                                        event.target.value,
                                    )
                                }
                                autoComplete="street-address"
                                required
                                rows={4}
                                className="resize-y rounded-xl border bg-background px-3 py-3 outline-none focus:ring-2 focus:ring-ring"
                            />
                        </label>

                        <label className="grid gap-2">
                            <span className="text-sm font-medium">
                                {getText(
                                    'checkout.city',
                                    'City',
                                )}
                            </span>

                            <input
                                value={city}
                                onChange={event =>
                                    setCity(
                                        event.target.value,
                                    )
                                }
                                autoComplete="address-level2"
                                required
                                className="h-11 rounded-xl border bg-background px-3 outline-none focus:ring-2 focus:ring-ring"
                            />
                        </label>

                        <label className="grid gap-2">
                            <span className="text-sm font-medium">
                                {getText(
                                    'checkout.postalCode',
                                    'Postal code',
                                )}
                            </span>

                            <input
                                value={postalCode}
                                onChange={event =>
                                    setPostalCode(
                                        event.target.value,
                                    )
                                }
                                autoComplete="postal-code"
                                inputMode="numeric"
                                className="h-11 rounded-xl border bg-background px-3 outline-none focus:ring-2 focus:ring-ring"
                            />
                        </label>
                    </div>
                </div>

                <div className="rounded-2xl border p-5 md:p-6">
                    <div className="flex items-center gap-3">
                        <Truck className="size-5" />

                        <div>
                            <h2 className="font-semibold">
                                {getText(
                                    'checkout.shippingMethod',
                                    'Shipping method',
                                )}
                            </h2>

                            <p className="text-sm text-muted-foreground">
                                {getText(
                                    'checkout.shippingMethodHint',
                                    'Choose how you want to receive your order.',
                                )}
                            </p>
                        </div>
                    </div>

                    {shippingMethodsQuery.isLoading ? (
                        <div className="mt-6 space-y-3">
                            <SkeletonLine className="h-20 w-full" />
                            <SkeletonLine className="h-20 w-full" />
                        </div>
                    ) : shippingMethodsQuery.isError ? (
                        <div className="mt-6 rounded-xl border p-4">
                            <p className="font-medium">
                                {getText(
                                    'checkout.shippingError',
                                    'Shipping methods could not be loaded.',
                                )}
                            </p>

                            <button
                                type="button"
                                onClick={() =>
                                    shippingMethodsQuery.refetch()
                                }
                                className="mt-3 rounded-lg border px-4 py-2 text-sm font-medium"
                            >
                                {getText(
                                    'common.retry',
                                    'Retry',
                                )}
                            </button>
                        </div>
                    ) : shippingMethodsQuery.data?.length ? (
                        <div className="mt-6 space-y-3">
                            {shippingMethodsQuery.data.map(
                                method => {
                                    const selected =
                                        method.id ===
                                        shippingMethodId;

                                    return (
                                        <label
                                            key={method.id}
                                            className={`block cursor - pointer rounded - xl border p - 4 transition ${
        selected
            ? 'border-primary ring-2 ring-primary/20'
            : 'hover:border-foreground/30'
    } `}
                                        >
                                            <input
                                                type="radio"
                                                name="shippingMethod"
                                                value={method.id}
                                                checked={selected}
                                                onChange={event =>
                                                    setShippingMethodId(
                                                        event.target.value,
                                                    )
                                                }
                                                className="sr-only"
                                            />

                                            <div className="flex items-center justify-between gap-4">
                                                <div>
                                                    <div className="font-medium">
                                                        {method.name}
                                                    </div>

                                                    <div className="mt-1 text-sm text-muted-foreground">
                                                        {method.carrier}
                                                    </div>
                                                </div>

                                                <div className="text-right">
                                                    <div className="font-semibold">
                                                        {formatMoney(
                                                            method.price,
                                                            cart.currency,
                                                        )}
                                                    </div>
                                                </div>
                                            </div>
                                        </label>
                                    );
                                },
                            )}
                        </div>
                    ) : (
                        <div className="mt-6 rounded-xl border p-4 text-sm text-muted-foreground">
                            {getText(
                                'checkout.noShippingMethods',
                                'No shipping methods are currently available.',
                            )}
                        </div>
                    )}
                </div>
            </section>

            <aside className="h-fit rounded-2xl border p-5 md:p-6 lg:sticky lg:top-6">
                <h2 className="text-lg font-semibold">
                    {getText(
                        'checkout.summary',
                        'Order summary',
                    )}
                </h2>

                <div className="mt-5 space-y-3">
                    <div className="flex justify-between gap-4 text-sm">
                        <span className="text-muted-foreground">
                            {getText(
                                'checkout.subtotal',
                                'Subtotal',
                            )}
                        </span>

                        <span className="font-medium">
                            {formatMoney(
                                subtotal,
                                cart.currency,
                            )}
                        </span>
                    </div>

                    <div className="flex justify-between gap-4 text-sm">
                        <span className="text-muted-foreground">
                            {getText(
                                'checkout.shipping',
                                'Shipping',
                            )}
                        </span>

                        <span className="font-medium">
                            {formatMoney(
                                shippingAmount,
                                cart.currency,
                            )}
                        </span>
                    </div>

                    <div className="border-t pt-4">
                        <div className="flex items-center justify-between gap-4">
                            <span className="font-semibold">
                                {getText(
                                    'checkout.total',
                                    'Total',
                                )}
                            </span>

                            <span className="text-xl font-bold">
                                {formatMoney(
                                    estimatedTotal,
                                    cart.currency,
                                )}
                            </span>
                        </div>
                    </div>
                </div>

                <button
                    type="submit"
                    disabled={
                        checkout.isPending ||
                        shippingMethodsQuery.isLoading ||
                        shippingMethodsQuery.isError
                    }
                    className="mt-6 flex h-12 w-full items-center justify-center gap-2 rounded-xl bg-primary px-4 font-semibold text-primary-foreground transition hover:opacity-90 disabled:cursor-not-allowed disabled:opacity-50"
                >
                    {checkout.isPending
                        ? getText(
                              'checkout.processing',
                              'Processing...',
                          )
                        : getText(
                              'checkout.placeOrder',
                              'Place order',
                          )}
                </button>

                <p className="mt-3 text-center text-xs text-muted-foreground">
                    {getText(
                        'checkout.secureHint',
                        'Your order will be created securely on the server.',
                    )}
                </p>
            </aside>
        </form>
    </div>
);


}
