import {
    Minus,
    Plus,
    ShoppingBag,
    Trash2,
} from 'lucide-react';

import { Link } from 'react-router-dom';

import { useCart } from '@/modules/cart/hooks/useCart';
import { useCartMutations } from '@/modules/cart/hooks/useCartMutations';

function formatMoney(
    amount: number,
    currency: string,
) {
    return new Intl.NumberFormat(undefined, {
        maximumFractionDigits: 0,
    }).format(amount) + ` ${currency}`;
}

export default function CartPage() {
    const { data: cart, isLoading } = useCart();

    const {
        setQuantity,
        remove,
        clear,
    } = useCartMutations();

    if (isLoading) {
        return (
            <div className="mx-auto max-w-6xl p-6">
                Loading cart...
            </div>
        );
    }

    if (!cart || cart.items.length === 0) {
        return (
            <div className="mx-auto max-w-6xl p-6">
                <div className="rounded-2xl border p-12 text-center">
                    <ShoppingBag className="mx-auto mb-4 size-12 opacity-50" />

                    <h1 className="text-2xl font-semibold">
                        Your cart is empty
                    </h1>

                    <p className="mt-2 text-muted-foreground">
                        Add products to your cart to continue.
                    </p>

                    <Link
                        to="/products"
                        className="mt-6 inline-flex rounded-lg border px-5 py-3 font-medium"
                    >
                        Continue shopping
                    </Link>
                </div>
            </div>
        );
    }

    return (
        <div className="mx-auto max-w-6xl p-6">
            <div className="mb-8 flex items-center justify-between">
                <div>
                    <h1 className="text-3xl font-bold">
                        Shopping Cart
                    </h1>

                    <p className="mt-1 text-muted-foreground">
                        {cart.items.length} item(s)
                    </p>
                </div>

                <button
                    type="button"
                    onClick={() => clear.mutate()}
                    disabled={clear.isPending}
                    className="inline-flex items-center gap-2 rounded-lg border px-4 py-2"
                >
                    <Trash2 className="size-4" />
                    Clear cart
                </button>
            </div>

            <div className="grid gap-6 lg:grid-cols-[1fr_360px]">
                <div className="space-y-4">
                    {cart.items.map((item) => (
                        <div
                            key={item.productVariantId}
                            className="rounded-2xl border p-5"
                        >
                            <div className="flex gap-4">
                                <div className="size-24 shrink-0 rounded-xl bg-muted" />

                                <div className="min-w-0 flex-1">
                                    <h2 className="font-semibold">
                                        {item.productName}
                                    </h2>

                                    <div className="mt-1 text-sm text-muted-foreground">
                                        SKU: {item.sku}
                                    </div>

                                    <div className="mt-2">
                                        {formatMoney(
                                            item.unitPrice,
                                            cart.currency,
                                        )}
                                    </div>

                                    <div className="mt-4 flex items-center justify-between">
                                        <div className="flex items-center rounded-lg border">
                                            <button
                                                type="button"
                                                className="p-2"
                                                disabled={
                                                    item.quantity <= 1 ||
                                                    setQuantity.isPending
                                                }
                                                onClick={() =>
                                                    setQuantity.mutate({
                                                        productVariantId:
                                                            item.productVariantId,
                                                        quantity:
                                                            item.quantity - 1,
                                                    })
                                                }
                                            >
                                                <Minus className="size-4" />
                                            </button>

                                            <span className="min-w-10 text-center">
                                                {item.quantity}
                                            </span>

                                            <button
                                                type="button"
                                                className="p-2"
                                                disabled={
                                                    item.quantity >=
                                                    item.availableStock ||
                                                    setQuantity.isPending
                                                }
                                                onClick={() =>
                                                    setQuantity.mutate({
                                                        productVariantId:
                                                            item.productVariantId,
                                                        quantity:
                                                            item.quantity + 1,
                                                    })
                                                }
                                            >
                                                <Plus className="size-4" />
                                            </button>
                                        </div>

                                        <button
                                            type="button"
                                            className="text-destructive"
                                            onClick={() =>
                                                remove.mutate(
                                                    item.productVariantId,
                                                )
                                            }
                                        >
                                            Remove
                                        </button>
                                    </div>
                                </div>

                                <div className="text-right font-semibold">
                                    {formatMoney(
                                        item.lineTotal,
                                        cart.currency,
                                    )}
                                </div>
                            </div>
                        </div>
                    ))}
                </div>

                <aside className="h-fit rounded-2xl border p-6">
                    <h2 className="text-xl font-semibold">
                        Order Summary
                    </h2>

                    <div className="mt-6 flex justify-between">
                        <span>Subtotal</span>

                        <span>
                            {formatMoney(
                                cart.subtotal,
                                cart.currency,
                            )}
                        </span>
                    </div>

                    <div className="mt-3 flex justify-between text-sm text-muted-foreground">
                        <span>Shipping</span>
                        <span>Calculated at checkout</span>
                    </div>

                    <div className="my-6 border-t" />

                    <div className="flex justify-between text-lg font-bold">
                        <span>Total</span>

                        <span>
                            {formatMoney(
                                cart.totalAmount,
                                cart.currency,
                            )}
                        </span>
                    </div>

                    <Link
                        to="/checkout"
                        className="mt-6 flex w-full items-center justify-center rounded-xl border px-5 py-3 font-semibold"
                    >
                        Proceed to checkout
                    </Link>
                </aside>
            </div>
        </div>
    );
}