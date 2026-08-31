import { useMemo, useState } from 'react';
import type {
    FormEvent,
} from 'react';

import { useNavigate } from 'react-router-dom';
import { useMutation } from '@tanstack/react-query';

import { useCart } from '@/modules/cart/hooks/useCart';
import { checkout } from '@/modules/checkout/api/checkoutApi';

function createIdempotencyKey() {
    return crypto.randomUUID();
}

export default function CheckoutPage() {
    const navigate = useNavigate();

    const { data: cart, isLoading } = useCart();

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

    const mutation = useMutation({
        mutationFn: async () => {
            if (!cart) {
                throw new Error(
                    'Cart is unavailable.',
                );
            }

            return checkout(
                {
                    items: cart.items.map((item) => ({
                        productVariantId:
                            item.productVariantId,
                        quantity: item.quantity,
                    })),

                    shippingFullName:
                        fullName.trim(),

                    shippingPhone:
                        phone.trim(),

                    shippingAddress:
                        address.trim(),

                    shippingCity:
                        city.trim(),

                    shippingPostalCode:
                        postalCode.trim() || null,

                    shippingAmount: 0,
                },

                createIdempotencyKey(),
            );
        },

        onSuccess: (order) => {
            navigate(
                `/orders/payment/${order.id}`,
            );
        },
    });

    const subtotal = useMemo(
        () => cart?.subtotal ?? 0,
        [cart],
    );

    if (isLoading) {
        return (
            <div className="mx-auto max-w-6xl p-6">
                Loading checkout...
            </div>
        );
    }

    if (!cart || cart.items.length === 0) {
        navigate('/cart', {
            replace: true,
        });

        return null;
    }

    function submit(
        event: FormEvent,
    ) {
        event.preventDefault();
        mutation.mutate();
    }

    return (
        <div className="mx-auto max-w-6xl p-6">
            <div className="mb-8">
                <h1 className="text-3xl font-bold">
                    Checkout
                </h1>

                <p className="mt-1 text-muted-foreground">
                    Complete your shipping information.
                </p>
            </div>

            <form
                onSubmit={submit}
                className="grid gap-6 lg:grid-cols-[1fr_360px]"
            >
                <div className="rounded-2xl border p-6">
                    <h2 className="text-xl font-semibold">
                        Shipping information
                    </h2>

                    <div className="mt-6 grid gap-4">
                        <input
                            required
                            value={fullName}
                            onChange={(e) =>
                                setFullName(e.target.value)
                            }
                            placeholder="Full name"
                            className="rounded-lg border px-4 py-3"
                        />

                        <input
                            required
                            value={phone}
                            onChange={(e) =>
                                setPhone(e.target.value)
                            }
                            placeholder="Phone number"
                            className="rounded-lg border px-4 py-3"
                        />

                        <input
                            required
                            value={city}
                            onChange={(e) =>
                                setCity(e.target.value)
                            }
                            placeholder="City"
                            className="rounded-lg border px-4 py-3"
                        />

                        <input
                            value={postalCode}
                            onChange={(e) =>
                                setPostalCode(e.target.value)
                            }
                            placeholder="Postal code"
                            className="rounded-lg border px-4 py-3"
                        />

                        <textarea
                            required
                            rows={5}
                            value={address}
                            onChange={(e) =>
                                setAddress(e.target.value)
                            }
                            placeholder="Full shipping address"
                            className="rounded-lg border px-4 py-3"
                        />
                    </div>

                    {mutation.error && (
                        <div className="mt-5 rounded-lg border p-4 text-sm text-destructive">
                            {mutation.error instanceof Error
                                ? mutation.error.message
                                : 'Checkout failed.'}
                        </div>
                    )}

                    <button
                        type="submit"
                        disabled={mutation.isPending}
                        className="mt-6 w-full rounded-xl border px-5 py-3 font-semibold"
                    >
                        {mutation.isPending
                            ? 'Creating order...'
                            : 'Place order'}
                    </button>
                </div>

                <aside className="h-fit rounded-2xl border p-6">
                    <h2 className="text-xl font-semibold">
                        Summary
                    </h2>

                    <div className="mt-6 space-y-3">
                        {cart.items.map((item) => (
                            <div
                                key={item.productVariantId}
                                className="flex justify-between gap-4 text-sm"
                            >
                                <span>
                                    {item.productName} × {item.quantity}
                                </span>

                                <span>
                                    {item.lineTotal.toLocaleString()}
                                </span>
                            </div>
                        ))}
                    </div>

                    <div className="my-6 border-t" />

                    <div className="flex justify-between text-lg font-bold">
                        <span>Total</span>
                        <span>
                            {subtotal.toLocaleString()} {cart.currency}
                        </span>
                    </div>
                </aside>
            </form>
        </div>
    );
}