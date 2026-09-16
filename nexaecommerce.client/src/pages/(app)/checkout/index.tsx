import {
    type FormEvent,
    useEffect,
    useRef,
    useState,
} from 'react';

import {
    AlertCircle,
    Check,
    MapPin,
    RefreshCw,
    Truck,
} from 'lucide-react';

import {
    Link,
    useNavigate,
} from 'react-router-dom';

import {
    useTranslation,
} from 'react-i18next';

import {
    useCart,
} from '@/modules/cart/hooks/useCart';

import {
    useCheckout,
} from '@/modules/orders/hooks/useCheckout';

import {
    useShippingMethods,
} from '@/modules/orders/hooks/useShippingMethods';

import {
    useAuth,
} from '@/hooks/use-auth';

import type {
    CheckoutRequest,
} from '@/modules/orders/types';

function formatMoney(
    amount: number,
    currency: string,
    isFa: boolean,
) {
    return (
        new Intl.NumberFormat(
            isFa
                ? 'fa-IR'
                : undefined,
            {
                maximumFractionDigits: 0,
           },
        ).format(amount) +
       ` ${currency}`
    );
}

function getErrorMessage(
    error: unknown,
    fallback: string,
): string {
    if (
        error instanceof Error &&
        error.message.trim()
    ) {
        return error.message;
   }

    return fallback;
}

function SkeletonLine({
    className = '',
}: {
    className?: string;
}) {
    return (
        <div
            className={`animate - pulse rounded - lg bg - muted ${className}`}
       />
    );
}

export default function CheckoutPage() {
    const {
        t,
        i18n,
   } = useTranslation();

    const navigate =
        useNavigate();

    const isFa =
        i18n.language
            ?.toLowerCase()
            .startsWith('fa');

    const {
        isAuthenticated,
        isLoading:
            authLoading,
   } = useAuth();

    useEffect(() => {
        if (
            authLoading ||
            isAuthenticated
        ) {
            return;
       }

        navigate(
            '/login?returnUrl=%2Fcheckout',
            {
                replace: true,
           },
        );
   }, [
        authLoading,
        isAuthenticated,
        navigate,
    ]);

    const cartQuery =
        useCart();

    const shippingMethodsQuery =
        useShippingMethods();

    const checkout =
        useCheckout();

    const checkoutKeyRef =
        useRef<string | null>(
            null,
        );

    const [
        fullName,
        setFullName,
    ] = useState('');

    const [
        phone,
        setPhone,
    ] = useState('');

    const [
        address,
        setAddress,
    ] = useState('');

    const [
        city,
        setCity,
    ] = useState('');

    const [
        postalCode,
        setPostalCode,
    ] = useState('');

    const [
        shippingMethodId,
        setShippingMethodId,
    ] = useState('');

    const [
        validationError,
        setValidationError,
    ] = useState<string | null>(
        null,
    );

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

    const activeShippingMethods =
        (
            shippingMethodsQuery.data ??
            []
        ).filter(
            method =>
                method.isActive,
        );

   /*
     * Do not set state from an effect just to select
     * the first shipping method. The first active method
     * acts as the effective selection until the user
     * explicitly chooses another method.
     */
    const effectiveShippingMethodId =
        shippingMethodId ||
        activeShippingMethods[0]?.id ||
        '';

    const selectedShippingMethod =
        activeShippingMethods.find(
            method =>
                method.id ===
                effectiveShippingMethodId,
        );

    const handleSubmit = (
        event: FormEvent<HTMLFormElement>,
    ) => {
        event.preventDefault();

        setValidationError(
            null,
        );

        const cart =
            cartQuery.data;

        if (
            cartQuery.isError ||
            !cart
        ) {
            setValidationError(
                getText(
                    'checkout.cartError',
                    'Your cart could not be loaded. Please try again.',
                ),
            );

            return;
       }

        if (
            cart.items.length ===
            0
        ) {
            setValidationError(
                getText(
                    'checkout.emptyCart',
                    'Your shopping cart is empty.',
                ),
            );

            return;
       }

        if (
            !isAuthenticated
        ) {
            navigate(
                '/login?returnUrl=%2Fcheckout',
                {
                    replace: true,
               },
            );

            return;
       }

        if (
            !fullName.trim()
        ) {
            setValidationError(
                getText(
                    'checkout.validation.fullName',
                    'Full name is required.',
                ),
            );

            return;
       }

        if (
            fullName.trim().length <
            2
        ) {
            setValidationError(
                getText(
                    'checkout.validation.fullNameShort',
                    'Please enter a valid full name.',
                ),
            );

            return;
       }

        if (
            !phone.trim()
        ) {
            setValidationError(
                getText(
                    'checkout.validation.phone',
                    'Phone number is required.',
                ),
            );

            return;
       }

        if (
            phone.trim().length <
            7
        ) {
            setValidationError(
                getText(
                    'checkout.validation.phoneInvalid',
                    'Please enter a valid phone number.',
                ),
            );

            return;
       }

        if (
            !address.trim()
        ) {
            setValidationError(
                getText(
                    'checkout.validation.address',
                    'Address is required.',
                ),
            );

            return;
       }

        if (
            address.trim().length <
            5
        ) {
            setValidationError(
                getText(
                    'checkout.validation.addressShort',
                    'Please enter a complete shipping address.',
                ),
            );

            return;
       }

        if (
            !city.trim()
        ) {
            setValidationError(
                getText(
                    'checkout.validation.city',
                    'City is required.',
                ),
            );

            return;
       }

        if (
            !effectiveShippingMethodId
        ) {
            setValidationError(
                getText(
                    'checkout.validation.shipping',
                    'Please select a shipping method.',
                ),
            );

            return;
       }

        if (
            !selectedShippingMethod
        ) {
            setValidationError(
                getText(
                    'checkout.validation.shippingInvalid',
                    'The selected shipping method is no longer available. Please choose another one.',
                ),
            );

            if (
                shippingMethodId
            ) {
                setShippingMethodId(
                    '',
                );
           }

            return;
       }

        if (
            !checkoutKeyRef.current
        ) {
            checkoutKeyRef.current =
                crypto.randomUUID();
       }

        const request:
            CheckoutRequest = {
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

            shippingMethodId:
                selectedShippingMethod.id,
       };

        checkout.mutate(
            {
                request,
                idempotencyKey:
                    checkoutKeyRef.current,
           },
            {
                onSuccess:
                    order => {
                        navigate(
                           `/orders/payment/${order.id}`,
                            {
                                replace: true,
                           },
                        );
                   },

                onError:
                    checkoutError => {
                        setValidationError(
                            getErrorMessage(
                                checkoutError,
                                getText(
                                    'checkout.error',
                                    'Unable to create the order. Please check your information and try again.',
                                ),
                            ),
                        );
                   },
           },
        );
   };

    const cart =
        cartQuery.data;

    if (
        authLoading ||
        !isAuthenticated
    ) {
        return (
            <div
                className="mx-auto max-w-7xl space-y-4 p-4 md:p-6"
                dir={
                    isFa
                        ? 'rtl'
                        : 'ltr'
               }
            >
                <SkeletonLine className="h-5 w-32"/>

                <SkeletonLine className="h-10 w-48"/>

                <div className="grid gap-6 lg:grid-cols-[1fr_380px]">
                    <SkeletonLine className="h-[560px] w-full"/>

                    <SkeletonLine className="h-[360px] w-full"/>
                </div>
            </div>
        );
   }

    if (
        cartQuery.isLoading ||
        shippingMethodsQuery.isLoading
    ) {
        return (
            <div
                className="mx-auto max-w-7xl space-y-4 p-4 md:p-6"
                dir={
                    isFa
                        ? 'rtl'
                        : 'ltr'
               }
            >
                <SkeletonLine className="h-5 w-32"/>

                <SkeletonLine className="h-10 w-48"/>

                <div className="grid gap-6 lg:grid-cols-[1fr_380px]">
                    <div className="space-y-6">
                        <SkeletonLine className="h-72 w-full"/>

                        <SkeletonLine className="h-52 w-full"/>
                    </div>

                    <SkeletonLine className="h-[360px] w-full"/>
                </div>
            </div>
        );
   }

    if (
        cartQuery.isError
    ) {
        return (
            <div
                dir={
                    isFa
                        ? 'rtl'
                        : 'ltr'
               }
                className="mx-auto max-w-3xl p-6"
            >
                <div className="rounded-2xl border border-destructive/30 p-10 text-center">
                    <AlertCircle className="mx-auto size-12 text-destructive"/>

                    <h1 className="mt-4 text-2xl font-semibold">
                        {getText(
                            'checkout.cartError',
                            'Unable to load your cart.',
                        )}
                    </h1>

                    <p className="mt-2 text-sm text-muted-foreground">
                        {getErrorMessage(
                            cartQuery.error,
                            getText(
                                'checkout.cartErrorDescription',
                                'Please try again before continuing to checkout.',
                            ),
                        )}
                    </p>

                    <button
                        type="button"
                        onClick={() =>
                            cartQuery.refetch()
                       }
                        disabled={
                            cartQuery.isFetching
                       }
                        className="mt-6 inline-flex items-center gap-2 rounded-xl border px-5 py-3 font-medium"
                    >
                        <RefreshCw
                            className={
                                cartQuery.isFetching
                                    ? 'size-4 animate-spin'
                                    : 'size-4'
                           }
                       />

                        {getText(
                            'common.retry',
                            'Retry',
                        )}
                    </button>

                    <Link
                        to="/cart"
                        className="mt-3 inline-flex rounded-xl px-5 py-3 text-sm font-medium"
                    >
                        {getText(
                            'checkout.backToCart',
                            'Back to cart',
                        )}
                    </Link>
                </div>
            </div>
        );
   }

    if (
        !cart ||
        cart.items.length ===
            0
    ) {
        return (
            <div
                dir={
                    isFa
                        ? 'rtl'
                        : 'ltr'
               }
                className="mx-auto max-w-3xl p-6"
            >
                <div className="rounded-2xl border p-10 text-center">
                    <h1 className="text-2xl font-bold">
                        {getText(
                            'checkout.emptyCart',
                            'Your shopping cart is empty.',
                        )}
                    </h1>

                    <Link
                        to="/products"
                        className="mt-6 inline-flex rounded-xl border px-5 py-3 font-medium"
                    >
                        {getText(
                            'common.continueShopping',
                            'Continue shopping',
                        )}
                    </Link>
                </div>
            </div>
        );
   }

    if (
        shippingMethodsQuery.isError
    ) {
        return (
            <div
                dir={
                    isFa
                        ? 'rtl'
                        : 'ltr'
               }
                className="mx-auto max-w-3xl p-6"
            >
                <div className="rounded-2xl border border-destructive/30 p-10 text-center">
                    <AlertCircle className="mx-auto size-12 text-destructive"/>

                    <h1 className="mt-4 text-2xl font-semibold">
                        {getText(
                            'checkout.shippingError',
                            'Shipping methods could not be loaded.',
                        )}
                    </h1>

                    <p className="mt-2 text-sm text-muted-foreground">
                        {getErrorMessage(
                            shippingMethodsQuery.error,
                            getText(
                                'checkout.shippingErrorDescription',
                                'Please try again before placing your order.',
                            ),
                        )}
                    </p>

                    <button
                        type="button"
                        onClick={() =>
                            shippingMethodsQuery.refetch()
                       }
                        disabled={
                            shippingMethodsQuery.isFetching
                       }
                        className="mt-6 inline-flex items-center gap-2 rounded-xl border px-5 py-3 font-medium"
                    >
                        <RefreshCw
                            className={
                                shippingMethodsQuery.isFetching
                                    ? 'size-4 animate-spin'
                                    : 'size-4'
                           }
                       />

                        {getText(
                            'common.retry',
                            'Retry',
                        )}
                    </button>
                </div>
            </div>
        );
   }

    const subtotal =
        cart.subtotal;

    const shippingAmount =
        selectedShippingMethod?.price ??
        0;

    const estimatedTotal =
        subtotal +
        shippingAmount;

    const submitDisabled =
        checkout.isPending ||
        !activeShippingMethods.length ||
        !effectiveShippingMethodId;

    return (
        <div
            dir={
                isFa
                    ? 'rtl'
                    : 'ltr'
           }
            className="mx-auto max-w-7xl space-y-6 p-4 md:p-6"
        >
            <div className="flex items-center gap-2 text-sm text-muted-foreground">
                <Link
                    to="/cart"
                    className="hover:text-foreground"
                >
                    {getText(
                        'cart.title',
                        'Cart',
                    )}
                </Link>

                <span>/</span>

                <span className="font-medium text-foreground">
                    {getText(
                        'checkout.title',
                        'Checkout',
                    )}
                </span>
            </div>

            <div>
                <h1 className="text-3xl font-bold">
                    {getText(
                        'checkout.title',
                        'Checkout',
                    )}
                </h1>

                <p className="mt-1 text-sm text-muted-foreground">
                    {getText(
                        'checkout.subtitle',
                        'Review your delivery information and choose a shipping method.',
                    )}
                </p>
            </div>

            {validationError && (
                <div
                    role="alert"
                    className="rounded-xl border border-destructive/50 bg-destructive/10 p-4 text-sm text-destructive"
                >
                    {validationError}
                </div>
            )}

            <form
                onSubmit={handleSubmit}
                className="grid gap-6 lg:grid-cols-[1fr_380px]"
            >
                <section className="space-y-6">
                    <div className="rounded-2xl border bg-card p-5 md:p-6">
                        <div className="flex items-center gap-3">
                            <MapPin className="size-5"/>

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
                                    disabled={
                                        checkout.isPending
                                   }
                                    className="h-11 rounded-xl border bg-background px-3 outline-none focus:ring-2 focus:ring-ring disabled:cursor-not-allowed disabled:opacity-60"
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
                                    disabled={
                                        checkout.isPending
                                   }
                                    className="h-11 rounded-xl border bg-background px-3 outline-none focus:ring-2 focus:ring-ring disabled:cursor-not-allowed disabled:opacity-60"
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
                                    disabled={
                                        checkout.isPending
                                   }
                                    className="resize-y rounded-xl border bg-background px-3 py-3 outline-none focus:ring-2 focus:ring-ring disabled:cursor-not-allowed disabled:opacity-60"
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
                                    disabled={
                                        checkout.isPending
                                   }
                                    className="h-11 rounded-xl border bg-background px-3 outline-none focus:ring-2 focus:ring-ring disabled:cursor-not-allowed disabled:opacity-60"
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
                                    disabled={
                                        checkout.isPending
                                   }
                                    className="h-11 rounded-xl border bg-background px-3 outline-none focus:ring-2 focus:ring-ring disabled:cursor-not-allowed disabled:opacity-60"
                               />
                            </label>
                        </div>
                    </div>

                    <div className="rounded-2xl border bg-card p-5 md:p-6">
                        <div className="flex items-center gap-3">
                            <Truck className="size-5"/>

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

                        {!activeShippingMethods.length ? (
                            <div className="mt-6 rounded-xl border border-dashed p-5 text-sm text-muted-foreground">
                                {getText(
                                    'checkout.noShippingMethods',
                                    'No shipping methods are currently available.',
                                )}
                            </div>
                        ) : (
                            <div className="mt-6 space-y-3">
                                {activeShippingMethods.map(
                                    method => {
                                        const selected =
                                            method.id ===
                                            effectiveShippingMethodId;

                                        return (
                                            <label
                                                key={
                                                    method.id
                                               }
                                                className={`block cursor - pointer rounded - xl border p - 4 transition ${
    selected
        ? 'border-primary bg-primary/5 ring-2 ring-primary/20'
        : 'hover:border-foreground/30'
} ${
    checkout.isPending
        ? 'cursor-not-allowed opacity-60'
        : ''
}`}
                                            >
                                                <input
                                                    type="radio"
                                                    name="shippingMethod"
                                                    value={
                                                        method.id
                                                   }
                                                    checked={
                                                        selected
                                                   }
                                                    onChange={event =>
                                                        setShippingMethodId(
                                                            event
                                                                .target
                                                                .value,
                                                        )
                                                   }
                                                    disabled={
                                                        checkout.isPending
                                                   }
                                                    className="sr-only"
                                               />

                                                <div className="flex items-center justify-between gap-4">
                                                    <div className="min-w-0">
                                                        <div className="flex items-center gap-2">
                                                            {selected && (
                                                                <span className="flex size-6 shrink-0 items-center justify-center rounded-full bg-primary text-primary-foreground">
                                                                    <Check className="size-4"/>
                                                                </span>
                                                            )}

                                                            <span className="font-medium">
                                                                {
                                                                    method.name
                                                               }
                                                            </span>
                                                        </div>

                                                        <div className="mt-1 text-sm text-muted-foreground">
                                                            {
                                                                method.carrier
                                                           }
                                                        </div>
                                                    </div>

                                                    <div className="shrink-0 text-end">
                                                        <div className="font-semibold">
                                                            {formatMoney(
                                                                method.price,
                                                                cart.currency,
                                                                isFa,
                                                            )}
                                                        </div>
                                                    </div>
                                                </div>
                                            </label>
                                        );
                                   },
                                )}
                            </div>
                        )}
                    </div>
                </section>

                <aside className="h-fit rounded-2xl border bg-card p-5 md:p-6 lg:sticky lg:top-6">
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
                                    'checkout.itemCount',
                                    'Items',
                                )}
                            </span>

                            <span className="font-medium">
                                {cart.items.reduce(
                                    (
                                        total,
                                        item,
                                    ) =>
                                        total +
                                        item.quantity,
                                    0,
                                )}
                            </span>
                        </div>

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
                                    isFa,
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
                                {selectedShippingMethod
                                    ? formatMoney(
                                          shippingAmount,
                                          cart.currency,
                                          isFa,
                                      )
                                    : getText(
                                          'checkout.selectShipping',
                                          'Select a method',
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
                                        isFa,
                                    )}
                                </span>
                            </div>
                        </div>
                    </div>

                    <button
                        type="submit"
                        disabled={
                            submitDisabled
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

                    <Link
                        to="/cart"
                        className="mt-3 flex w-full items-center justify-center rounded-xl border px-4 py-3 text-sm font-medium transition-colors hover:bg-muted"
                    >
                        {getText(
                            'checkout.backToCart',
                            'Back to cart',
                        )}
                    </Link>
                </aside>
            </form>
        </div>
    );
}
