import {
    type FormEvent,
    useEffect,
    useRef,
    useState,
} from 'react';
import { formatMoney } from '@/lib/formatMoney';
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
import { useTranslation } from 'react-i18next';

import { useCart } from '@/modules/cart/hooks/useCart';
import { useCheckout } from '@/modules/orders/hooks/useCheckout';
import { useShippingMethods } from '@/modules/orders/hooks/useShippingMethods';
import { useAuth } from '@/hooks/use-auth';
import { useCustomerAddresses } from '@/modules/customers/hooks/useCustomerAddresses';

import type { CustomerAddress } from '@/modules/customers/types';
import type { CheckoutRequest } from '@/modules/orders/types';

function SkeletonLine({ className = '' }: { className?: string }) {
    return (
        <div
            aria-hidden="true"
            className={`animate-pulse rounded-lg bg-muted ${className}`}
        />
    );
}

type AddressOptionProps = {
    address: CustomerAddress;
    selected: boolean;
    onSelect: () => void;
    isRtl: boolean;
};

function AddressOption({
    address,
    selected,
    onSelect,
    isRtl,
}: AddressOptionProps) {
    const { t } = useTranslation();

    return (
        <button
            type="button"
            onClick={onSelect}
            aria-pressed={selected}
            className={`w-full rounded-xl border p-4 text-start transition ${selected
                    ? 'border-primary bg-primary/5 ring-2 ring-primary/20'
                    : 'hover:border-foreground/30'
                }`}
        >
            <div className="flex items-start justify-between gap-4">
                <div className="min-w-0">
                    <div className="flex flex-wrap items-center gap-2">
                        <span className="font-semibold">{address.title}</span>
                        {address.isDefault && (
                            <span className="rounded-full border px-2 py-0.5 text-[11px] font-medium">
                                {t('storefront.checkout.default', {
                                    defaultValue: isRtl ? 'پیش‌فرض' : 'Default',
                                })}
                            </span>
                        )}
                    </div>

                    <div className="mt-2 font-medium">{address.recipientName}</div>
                    <div className="mt-1 text-sm text-muted-foreground">
                        {address.phoneNumber}
                    </div>
                    <div className="mt-2 text-sm leading-6 text-muted-foreground">
                        {address.addressLine}
                    </div>
                    <div className="text-sm text-muted-foreground">
                        {[address.city, address.province, address.country]
                            .filter(Boolean)
                            .join(isRtl ? '، ' : ', ')}
                    </div>
                    {address.postalCode && (
                        <div className="mt-1 text-sm text-muted-foreground">
                            {address.postalCode}
                        </div>
                    )}
                </div>

                <div
                    className={`mt-1 flex size-6 shrink-0 items-center justify-center rounded-full border ${selected
                            ? 'border-primary bg-primary text-primary-foreground'
                            : 'border-muted-foreground/40'
                        }`}
                    aria-hidden="true"
                >
                    {selected && <Check className="size-4" />}
                </div>
            </div>
        </button>
    );
}

export default function CheckoutPage() {
    const { t, i18n } = useTranslation();
    const navigate = useNavigate();

    const languageCode = (i18n.language ?? 'fa')
        .toLowerCase()
        .split('-')[0];
    const isRtl = languageCode === 'fa' || languageCode === 'ar';

    const getText = (key: string, fallback: string) =>
        t(key, { defaultValue: fallback });

    const { isAuthenticated, isLoading: authLoading } = useAuth();
    const addressesQuery = useCustomerAddresses(isAuthenticated);
    const cartQuery = useCart();
    const shippingMethodsQuery = useShippingMethods();
    const checkout = useCheckout();

    const checkoutKeyRef = useRef<string | null>(null);
    const defaultAddressAppliedRef = useRef(false);

    const [fullName, setFullName] = useState('');
    const [phone, setPhone] = useState('');
    const [address, setAddress] = useState('');
    const [city, setCity] = useState('');
    const [postalCode, setPostalCode] = useState('');
    const [selectedAddressId, setSelectedAddressId] = useState<string | null>(null);
    const [shippingMethodId, setShippingMethodId] = useState('');
    const [validationError, setValidationError] = useState<string | null>(null);

    const addresses = addressesQuery.data ?? [];
    const preferredAddress =
        addresses.find(item => item.isDefault) ?? addresses[0] ?? null;

    useEffect(() => {
        if (defaultAddressAppliedRef.current || !preferredAddress) {
            return;
        }

        const formAlreadyPopulated = Boolean(
            fullName.trim() ||
            phone.trim() ||
            address.trim() ||
            city.trim() ||
            postalCode.trim(),
        );

        if (formAlreadyPopulated) {
            defaultAddressAppliedRef.current = true;
            return;
        }

        setFullName(preferredAddress.recipientName);
        setPhone(preferredAddress.phoneNumber);
        setAddress(preferredAddress.addressLine);
        setCity(preferredAddress.city);
        setPostalCode(preferredAddress.postalCode ?? '');
        setSelectedAddressId(preferredAddress.id);
        defaultAddressAppliedRef.current = true;
    }, [preferredAddress, fullName, phone, address, city, postalCode]);

    useEffect(() => {
        if (authLoading || isAuthenticated) {
            return;
        }

        navigate('/login?returnUrl=%2Fcheckout', { replace: true });
    }, [authLoading, isAuthenticated, navigate]);

    const activeShippingMethods = (shippingMethodsQuery.data ?? []).filter(
        method => method.isActive,
    );

    const effectiveShippingMethodId =
        shippingMethodId || activeShippingMethods[0]?.id || '';
    const selectedShippingMethod = activeShippingMethods.find(
        method => method.id === effectiveShippingMethodId,
    );

    const applyAddress = (customerAddress: CustomerAddress) => {
        setSelectedAddressId(customerAddress.id);
        setFullName(customerAddress.recipientName);
        setPhone(customerAddress.phoneNumber);
        setAddress(customerAddress.addressLine);
        setCity(customerAddress.city);
        setPostalCode(customerAddress.postalCode ?? '');
        setValidationError(null);
    };

    const clearSelectedAddress = () => {
        setSelectedAddressId(null);
        setFullName('');
        setPhone('');
        setAddress('');
        setCity('');
        setPostalCode('');
        setValidationError(null);
    };

    const handleSubmit = (event: FormEvent<HTMLFormElement>) => {
        event.preventDefault();
        setValidationError(null);

        const cart = cartQuery.data;
        if (cartQuery.isError || !cart) {
            setValidationError(
                getText(
                    'storefront.checkout.cartError',
                    'Your cart could not be loaded. Please try again.',
                ),
            );
            return;
        }

        if (cart.items.length === 0) {
            setValidationError(
                getText('storefront.checkout.emptyCart', 'Your shopping cart is empty.'),
            );
            return;
        }

        if (!isAuthenticated) {
            navigate('/login?returnUrl=%2Fcheckout', { replace: true });
            return;
        }

        const validations: Array<[boolean, string, string]> = [
            [
                Boolean(fullName.trim()),
                'storefront.checkout.validation.fullNameRequired',
                'Full name is required.',
            ],
            [
                fullName.trim().length >= 2,
                'storefront.checkout.validation.fullNameInvalid',
                'Please enter a valid full name.',
            ],
            [
                Boolean(phone.trim()),
                'storefront.checkout.validation.phoneRequired',
                'Phone number is required.',
            ],
            [
                phone.trim().length >= 7,
                'storefront.checkout.validation.phoneInvalid',
                'Please enter a valid phone number.',
            ],
            [
                Boolean(address.trim()),
                'storefront.checkout.validation.addressRequired',
                'Address is required.',
            ],
            [
                address.trim().length >= 5,
                'storefront.checkout.validation.addressInvalid',
                'Please enter a complete shipping address.',
            ],
            [
                Boolean(city.trim()),
                'storefront.checkout.validation.cityRequired',
                'City is required.',
            ],
            [
                Boolean(effectiveShippingMethodId),
                'storefront.checkout.validation.shippingRequired',
                'Please select a shipping method.',
            ],
        ];

        for (const [isValid, key, fallback] of validations) {
            if (!isValid) {
                setValidationError(getText(key, fallback));
                return;
            }
        }

        if (!selectedShippingMethod) {
            setValidationError(
                getText(
                    'storefront.checkout.validation.shippingInvalid',
                    'The selected shipping method is no longer available. Please choose another one.',
                ),
            );
            if (shippingMethodId) {
                setShippingMethodId('');
            }
            return;
        }

        if (!checkoutKeyRef.current) {
            checkoutKeyRef.current = crypto.randomUUID();
        }

        const request: CheckoutRequest = {
            items: cart.items.map(item => ({
                productVariantId: item.productVariantId,
                quantity: item.quantity,
            })),
            shippingFullName: fullName.trim(),
            shippingPhone: phone.trim(),
            shippingAddress: address.trim(),
            shippingCity: city.trim(),
            shippingPostalCode: postalCode.trim() || null,
            shippingMethodId: selectedShippingMethod.id,
        };

        checkout.mutate(
            {
                request,
                idempotencyKey: checkoutKeyRef.current,
            },
            {
                onSuccess: order => {
                    navigate(`/orders/payment/${order.id}`, { replace: true });
                },
                onError: () => {
                    setValidationError(
                        getText(
                            'storefront.checkout.createOrderError',
                            'Unable to create the order. Please check your information and try again.',
                        ),
                    );
                },
            },
        );
    };

    const cart = cartQuery.data;

    if (authLoading || !isAuthenticated) {
        return (
            <div className="mx-auto max-w-7xl space-y-4 p-4 md:p-6" dir={isRtl ? 'rtl' : 'ltr'}>
                <SkeletonLine className="h-5 w-32" />
                <SkeletonLine className="h-10 w-48" />
                <div className="grid gap-6 lg:grid-cols-[1fr_380px]">
                    <SkeletonLine className="h-[560px] w-full" />
                    <SkeletonLine className="h-[360px] w-full" />
                </div>
            </div>
        );
    }

    if (cartQuery.isLoading || shippingMethodsQuery.isLoading) {
        return (
            <div className="mx-auto max-w-7xl space-y-4 p-4 md:p-6" dir={isRtl ? 'rtl' : 'ltr'}>
                <SkeletonLine className="h-5 w-32" />
                <SkeletonLine className="h-10 w-48" />
                <div className="grid gap-6 lg:grid-cols-[1fr_380px]">
                    <div className="space-y-6">
                        <SkeletonLine className="h-72 w-full" />
                        <SkeletonLine className="h-52 w-full" />
                    </div>
                    <SkeletonLine className="h-[360px] w-full" />
                </div>
            </div>
        );
    }

    if (cartQuery.isError) {
        return (
            <div dir={isRtl ? 'rtl' : 'ltr'} className="mx-auto max-w-3xl p-6">
                <div className="rounded-2xl border border-destructive/30 p-10 text-center">
                    <AlertCircle className="mx-auto size-12 text-destructive" />
                    <h1 className="mt-4 text-2xl font-semibold">
                        {getText('storefront.checkout.cartError', 'Unable to load your cart.')}
                    </h1>
                    <p className="mt-2 text-sm text-muted-foreground">
                        {getText(
                            'storefront.checkout.cartErrorDescription',
                            'Please try again before continuing to checkout.',
                        )}
                    </p>
                    <button
                        type="button"
                        onClick={() => cartQuery.refetch()}
                        disabled={cartQuery.isFetching}
                        className="mt-6 inline-flex items-center gap-2 rounded-xl border px-5 py-3 font-medium"
                    >
                        <RefreshCw className={cartQuery.isFetching ? 'size-4 animate-spin' : 'size-4'} />
                        {getText('common.retry', 'Retry')}
                    </button>
                    <Link to="/cart" className="mt-3 inline-flex rounded-xl px-5 py-3 text-sm font-medium">
                        {getText('storefront.checkout.backToCart', 'Back to cart')}
                    </Link>
                </div>
            </div>
        );
    }

    if (!cart || cart.items.length === 0) {
        return (
            <div dir={isRtl ? 'rtl' : 'ltr'} className="mx-auto max-w-3xl p-6">
                <div className="rounded-2xl border p-10 text-center">
                    <h1 className="text-2xl font-bold">
                        {getText('storefront.checkout.emptyCart', 'Your shopping cart is empty.')}
                    </h1>
                    <Link to="/products" className="mt-6 inline-flex rounded-xl border px-5 py-3 font-medium">
                        {getText('common.continueShopping', 'Continue shopping')}
                    </Link>
                </div>
            </div>
        );
    }

    if (shippingMethodsQuery.isError) {
        return (
            <div dir={isRtl ? 'rtl' : 'ltr'} className="mx-auto max-w-3xl p-6">
                <div className="rounded-2xl border border-destructive/30 p-10 text-center">
                    <AlertCircle className="mx-auto size-12 text-destructive" />
                    <h1 className="mt-4 text-2xl font-semibold">
                        {getText('storefront.checkout.shippingError', 'Shipping methods could not be loaded.')}
                    </h1>
                    <p className="mt-2 text-sm text-muted-foreground">
                        {getText(
                            'storefront.checkout.shippingErrorDescription',
                            'Please try again before placing your order.',
                        )}
                    </p>
                    <button
                        type="button"
                        onClick={() => shippingMethodsQuery.refetch()}
                        disabled={shippingMethodsQuery.isFetching}
                        className="mt-6 inline-flex items-center gap-2 rounded-xl border px-5 py-3 font-medium"
                    >
                        <RefreshCw className={shippingMethodsQuery.isFetching ? 'size-4 animate-spin' : 'size-4'} />
                        {getText('common.retry', 'Retry')}
                    </button>
                </div>
            </div>
        );
    }

    const subtotal = cart.subtotal;
    const shippingAmount = selectedShippingMethod?.price ?? 0;
    const estimatedTotal = subtotal + shippingAmount;
    const submitDisabled =
        checkout.isPending || !activeShippingMethods.length || !effectiveShippingMethodId;

    return (
        <div dir={isRtl ? 'rtl' : 'ltr'} className="mx-auto max-w-7xl space-y-6 p-4 md:p-6">
            <nav aria-label={getText('storefront.checkout.breadcrumb', 'Breadcrumb')} className="flex items-center gap-2 text-sm text-muted-foreground">
                <Link to="/cart" className="hover:text-foreground">
                    {getText('storefront.cart.title', 'Cart')}
                </Link>
                <span aria-hidden="true">/</span>
                <span className="font-medium text-foreground">
                    {getText('storefront.checkout.title', 'Checkout')}
                </span>
            </nav>

            <header>
                <h1 className="text-3xl font-bold">
                    {getText('storefront.checkout.title', 'Checkout')}
                </h1>
                <p className="mt-1 text-sm text-muted-foreground">
                    {getText(
                        'storefront.checkout.subtitle',
                        'Review your delivery information and choose a shipping method.',
                    )}
                </p>
            </header>

            {validationError && (
                <div role="alert" className="rounded-xl border border-destructive/50 bg-destructive/10 p-4 text-sm text-destructive">
                    {validationError}
                </div>
            )}

            <form onSubmit={handleSubmit} className="grid gap-6 lg:grid-cols-[minmax(0,1fr)_380px]">
                <section className="space-y-6">
                    {addresses.length > 0 && (
                        <div className="rounded-2xl border bg-card p-5 md:p-6">
                            <div className="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
                                <div>
                                    <div className="flex items-center gap-3">
                                        <MapPin className="size-5" aria-hidden="true" />
                                        <h2 className="font-semibold">
                                            {getText('storefront.checkout.savedAddresses', 'Saved addresses')}
                                        </h2>
                                    </div>
                                    <p className="mt-1 text-sm text-muted-foreground">
                                        {getText(
                                            'storefront.checkout.savedAddressesHint',
                                            'Choose a saved address or enter a different one below.',
                                        )}
                                    </p>
                                </div>
                                <button
                                    type="button"
                                    onClick={clearSelectedAddress}
                                    className={`rounded-lg border px-3 py-2 text-sm font-medium ${selectedAddressId === null ? 'border-primary bg-primary/5' : ''
                                        }`}
                                >
                                    {getText('storefront.checkout.manualAddress', 'Enter manually')}
                                </button>
                            </div>
                            <div className="mt-5 grid gap-3">
                                {addresses.map(item => (
                                    <AddressOption
                                        key={item.id}
                                        address={item}
                                        selected={selectedAddressId === item.id}
                                        onSelect={() => applyAddress(item)}
                                        isRtl={isRtl}
                                    />
                                ))}
                            </div>
                        </div>
                    )}

                    <div className="rounded-2xl border bg-card p-5 md:p-6">
                        <div className="flex items-center gap-3">
                            <MapPin className="size-5" aria-hidden="true" />
                            <div>
                                <h2 className="font-semibold">
                                    {getText('storefront.checkout.shippingAddress', 'Shipping address')}
                                </h2>
                                <p className="text-sm text-muted-foreground">
                                    {getText('storefront.checkout.shippingAddressHint', 'Where should we deliver your order?')}
                                </p>
                            </div>
                        </div>

                        <div className="mt-6 grid gap-4 md:grid-cols-2">
                            <label className="grid gap-2">
                                <span className="text-sm font-medium">
                                    {getText('storefront.checkout.fullName', 'Full name')}
                                </span>
                                <input
                                    value={fullName}
                                    onChange={event => {
                                        setFullName(event.target.value);
                                        setSelectedAddressId(null);
                                    }}
                                    autoComplete="name"
                                    required
                                    disabled={checkout.isPending}
                                    className="h-11 rounded-xl border bg-background px-3 outline-none focus:ring-2 focus:ring-ring disabled:cursor-not-allowed disabled:opacity-60"
                                />
                            </label>

                            <label className="grid gap-2">
                                <span className="text-sm font-medium">
                                    {getText('storefront.checkout.phone', 'Phone number')}
                                </span>
                                <input
                                    value={phone}
                                    onChange={event => {
                                        setPhone(event.target.value);
                                        setSelectedAddressId(null);
                                    }}
                                    autoComplete="tel"
                                    inputMode="tel"
                                    required
                                    disabled={checkout.isPending}
                                    className="h-11 rounded-xl border bg-background px-3 outline-none focus:ring-2 focus:ring-ring disabled:cursor-not-allowed disabled:opacity-60"
                                />
                            </label>

                            <label className="grid gap-2 md:col-span-2">
                                <span className="text-sm font-medium">
                                    {getText('storefront.checkout.address', 'Address')}
                                </span>
                                <textarea
                                    value={address}
                                    onChange={event => {
                                        setAddress(event.target.value);
                                        setSelectedAddressId(null);
                                    }}
                                    autoComplete="street-address"
                                    required
                                    rows={4}
                                    disabled={checkout.isPending}
                                    className="resize-y rounded-xl border bg-background px-3 py-3 outline-none focus:ring-2 focus:ring-ring disabled:cursor-not-allowed disabled:opacity-60"
                                />
                            </label>

                            <label className="grid gap-2">
                                <span className="text-sm font-medium">
                                    {getText('storefront.checkout.city', 'City')}
                                </span>
                                <input
                                    value={city}
                                    onChange={event => {
                                        setCity(event.target.value);
                                        setSelectedAddressId(null);
                                    }}
                                    autoComplete="address-level2"
                                    required
                                    disabled={checkout.isPending}
                                    className="h-11 rounded-xl border bg-background px-3 outline-none focus:ring-2 focus:ring-ring disabled:cursor-not-allowed disabled:opacity-60"
                                />
                            </label>

                            <label className="grid gap-2">
                                <span className="text-sm font-medium">
                                    {getText('storefront.checkout.postalCode', 'Postal code')}
                                </span>
                                <input
                                    value={postalCode}
                                    onChange={event => {
                                        setPostalCode(event.target.value);
                                        setSelectedAddressId(null);
                                    }}
                                    autoComplete="postal-code"
                                    inputMode="numeric"
                                    disabled={checkout.isPending}
                                    className="h-11 rounded-xl border bg-background px-3 outline-none focus:ring-2 focus:ring-ring disabled:cursor-not-allowed disabled:opacity-60"
                                />
                            </label>
                        </div>
                    </div>

                    <div className="rounded-2xl border bg-card p-5 md:p-6">
                        <div className="flex items-center gap-3">
                            <Truck className="size-5" aria-hidden="true" />
                            <div>
                                <h2 className="font-semibold">
                                    {getText('storefront.checkout.shippingMethod', 'Shipping method')}
                                </h2>
                                <p className="text-sm text-muted-foreground">
                                    {getText(
                                        'storefront.checkout.shippingMethodHint',
                                        'Choose how you want to receive your order.',
                                    )}
                                </p>
                            </div>
                        </div>

                        {activeShippingMethods.length === 0 ? (
                            <div className="mt-6 rounded-xl border border-dashed p-5 text-sm text-muted-foreground">
                                {getText(
                                    'storefront.checkout.noShippingMethods',
                                    'No shipping methods are currently available.',
                                )}
                            </div>
                        ) : (
                            <div className="mt-6 space-y-3">
                                {activeShippingMethods.map(method => {
                                    const selected = method.id === effectiveShippingMethodId;
                                    return (
                                        <label
                                            key={method.id}
                                            className={`block cursor-pointer rounded-xl border p-4 transition ${selected
                                                    ? 'border-primary bg-primary/5 ring-2 ring-primary/20'
                                                    : 'hover:border-foreground/30'
                                                } ${checkout.isPending ? 'cursor-not-allowed opacity-60' : ''}`}
                                        >
                                            <input
                                                type="radio"
                                                name="shippingMethod"
                                                value={method.id}
                                                checked={selected}
                                                onChange={event => setShippingMethodId(event.target.value)}
                                                disabled={checkout.isPending}
                                                className="sr-only"
                                            />
                                            <div className="flex items-center justify-between gap-4">
                                                <div className="min-w-0">
                                                    <div className="flex items-center gap-2">
                                                        {selected && (
                                                            <span className="flex size-6 shrink-0 items-center justify-center rounded-full bg-primary text-primary-foreground">
                                                                <Check className="size-4" />
                                                            </span>
                                                        )}
                                                        <span className="font-medium">{method.name}</span>
                                                    </div>
                                                    {method.carrier && (
                                                        <div className="mt-1 text-sm text-muted-foreground">
                                                            {method.carrier}
                                                        </div>
                                                    )}
                                                </div>
                                                <div className="shrink-0 text-end font-semibold">
                                                    {formatMoney(method.price, cart.currency, i18n.language)}
                                                </div>
                                            </div>
                                        </label>
                                    );
                                })}
                            </div>
                        )}
                    </div>

                    <div className="rounded-2xl border bg-card p-5 md:p-6">
                        <h2 className="font-semibold">
                            {getText('storefront.checkout.itemCount', 'Items')}
                        </h2>
                        <div className="mt-4 space-y-3">
                            {cart.items.map(item => (
                                <div
                                    key={item.productVariantId}
                                    className="rounded-xl border bg-background p-4"
                                >
                                    <div className="flex items-start justify-between gap-4">
                                        <div className="min-w-0">
                                            <div className="font-medium">{item.productName}</div>
                                            <div className="mt-1 text-xs text-muted-foreground">
                                                SKU: {item.sku}
                                            </div>
                                            {item.attributes?.length > 0 && (
                                                <div className="mt-3 flex flex-wrap gap-2">
                                                    {item.attributes.map(attribute => (
                                                        <span
                                                            key={`${item.productVariantId}-${attribute.attributeValueId}`}
                                                            className="inline-flex items-center gap-1 rounded-lg border bg-muted/40 px-2 py-1 text-xs"
                                                        >
                                                            <span className="font-semibold">
                                                                {attribute.attributeName}:
                                                            </span>
                                                            <span>
                                                                {attribute.displayValue ?? attribute.value}
                                                            </span>
                                                        </span>
                                                    ))}
                                                </div>
                                            )}
                                            <div className="mt-2 text-xs text-muted-foreground">
                                                × {item.quantity}
                                            </div>
                                        </div>
                                        <div className="shrink-0 text-sm font-semibold">
                                            {formatMoney(item.lineTotal, cart.currency, i18n.language)}
                                        </div>
                                    </div>
                                </div>
                            ))}
                        </div>
                    </div>
                </section>

                <aside className="h-fit rounded-2xl border bg-card p-5 md:p-6 lg:sticky lg:top-6">
                    <h2 className="text-lg font-semibold">
                        {getText('storefront.checkout.summary', 'Order summary')}
                    </h2>

                    <div className="mt-5 space-y-3">
                        <div className="flex justify-between gap-4 text-sm">
                            <span className="text-muted-foreground">
                                {getText('storefront.checkout.itemCount', 'Items')}
                            </span>
                            <span className="font-medium">
                                {cart.items.reduce((total, item) => total + item.quantity, 0)}
                            </span>
                        </div>

                        <div className="flex justify-between gap-4 text-sm">
                            <span className="text-muted-foreground">
                                {getText('storefront.checkout.subtotal', 'Subtotal')}
                            </span>
                            <span className="font-medium">
                                {formatMoney(subtotal, cart.currency, i18n.language)}
                            </span>
                        </div>

                        <div className="flex justify-between gap-4 text-sm">
                            <span className="text-muted-foreground">
                                {getText('storefront.checkout.shipping', 'Shipping')}
                            </span>
                            <span className="font-medium">
                                {selectedShippingMethod
                                    ? formatMoney(shippingAmount, cart.currency, i18n.language)
                                    : getText('storefront.checkout.selectShipping', 'Select a method')}
                            </span>
                        </div>

                        <div className="border-t pt-4">
                            <div className="flex items-center justify-between gap-4">
                                <span className="font-semibold">
                                    {getText('storefront.checkout.total', 'Total')}
                                </span>
                                <span className="text-xl font-bold">
                                    {formatMoney(estimatedTotal, cart.currency, i18n.language)}
                                </span>
                            </div>
                        </div>
                    </div>

                    <button
                        type="submit"
                        disabled={submitDisabled}
                        className="mt-6 flex h-12 w-full items-center justify-center gap-2 rounded-xl bg-primary px-4 font-semibold text-primary-foreground transition hover:opacity-90 disabled:cursor-not-allowed disabled:opacity-50"
                    >
                        {checkout.isPending
                            ? getText('storefront.checkout.processing', 'Processing...')
                            : getText('storefront.checkout.placeOrder', 'Place order')}
                    </button>

                    <Link
                        to="/cart"
                        className="mt-3 flex w-full items-center justify-center rounded-xl border px-4 py-3 text-sm font-medium"
                    >
                        {getText('storefront.checkout.backToCart', 'Back to cart')}
                    </Link>
                </aside>
            </form>
        </div>
    );
}
