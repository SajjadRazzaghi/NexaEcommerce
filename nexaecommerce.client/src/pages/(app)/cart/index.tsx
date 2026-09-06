import {
    Minus,
    Plus,
    ShoppingBag,
    Trash2,
    RefreshCw,
    AlertCircle,
} from 'lucide-react';

import {
    Link,
} from 'react-router-dom';

import {
    useCart,
} from '@/modules/cart/hooks/useCart';

import {
    useCartMutations,
} from '@/modules/cart/hooks/useCartMutations';


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
        ` ${ currency } `
    );
}


function getErrorMessage(
    error: unknown,
): string {
    if (
        error instanceof Error
    ) {
        return error.message;
    }

    return 'Something went wrong. Please try again.';
}


function LoadingCart() {
    return (
        <div className="mx-auto max-w-6xl space-y-4 p-6">
            <div className="h-9 w-52 animate-pulse rounded-lg bg-muted" />

            <div className="grid gap-6 lg:grid-cols-[1fr_360px]">
                <div className="space-y-4">
                    {Array.from(
                        {
                            length: 3,
                        },
                    ).map(
                        (
                            _,
                            index,
                        ) => (
                            <div
                                key={
                                    index
                                }
                                className="h-36 animate-pulse rounded-2xl bg-muted"
                            />
                        ),
                    )}
                </div>

                <div className="h-72 animate-pulse rounded-2xl bg-muted" />
            </div>
        </div>
    );
}


export default function CartPage() {
    const {
        data: cart,
        isLoading,
        isError,
        error,
        refetch,
        isFetching,
    } = useCart();


    const {
        setQuantity,
        remove,
        clear,
    } =
        useCartMutations();


    if (
        isLoading
    ) {
        return (
            <LoadingCart />
        );
    }


    if (
        isError
    ) {
        return (
            <div className="mx-auto max-w-3xl p-6">
                <div className="rounded-2xl border p-10 text-center">
                    <AlertCircle className="mx-auto size-12 text-destructive" />

                    <h1 className="mt-4 text-2xl font-semibold">
                        Unable to load your cart
                    </h1>

                    <p className="mt-2 text-sm text-muted-foreground">
                        {getErrorMessage(
                            error,
                        )}
                    </p>

                    <button
                        type="button"
                        onClick={() =>
                            refetch()
                        }
                        disabled={
                            isFetching
                        }
                        className="mt-6 inline-flex items-center gap-2 rounded-xl border px-5 py-3 font-medium"
                    >
                        <RefreshCw
                            className={
                                isFetching
                                    ? 'size-4 animate-spin'
                                    : 'size-4'
                            }
                        />

                        Retry
                    </button>
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
            <div className="mx-auto max-w-6xl p-6">
                <div className="rounded-2xl border p-12 text-center">
                    <ShoppingBag className="mx-auto mb-4 size-12 text-muted-foreground" />

                    <h1 className="text-2xl font-semibold">
                        Your cart is empty
                    </h1>

                    <p className="mt-2 text-muted-foreground">
                        Add products to your cart to continue.
                    </p>

                    <Link
                        to="/products"
                        className="mt-6 inline-flex rounded-xl border px-5 py-3 font-medium"
                    >
                        Continue shopping
                    </Link>
                </div>
            </div>
        );
    }


    const mutationPending =
        setQuantity.isPending ||
        remove.isPending ||
        clear.isPending;


    const canCheckout =
        cart.items.length >
        0;


    return (
        <div
            className="mx-auto max-w-6xl p-6"
            dir="auto"
        >
            <div className="mb-8 flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
                <div>
                    <h1 className="text-3xl font-bold">
                        Shopping Cart
                    </h1>

                    <p className="mt-1 text-muted-foreground">
                        {cart.items.length}{' '}
                        item(s)
                        {' · '}
                        {cart.items.reduce(
                            (
                                total,
                                item,
                            ) =>
                                total +
                                item.quantity,
                            0,
                        )}{' '}
                        unit(s)
                    </p>
                </div>

                <button
                    type="button"
                    onClick={() => {
                        if (
                            cart.items.length ===
                            0
                        ) {
                            return;
                        }

                        const confirmed =
                            window.confirm(
                                'Are you sure you want to clear your cart?',
                            );

                        if (
                            confirmed
                        ) {
                            clear.mutate();
                        }
                    }}
                    disabled={
                        mutationPending
                    }
                    className="inline-flex items-center justify-center gap-2 rounded-xl border px-4 py-2 font-medium disabled:cursor-not-allowed disabled:opacity-50"
                >
                    <Trash2 className="size-4" />
                    Clear cart
                </button>
            </div>


            <div className="grid gap-6 lg:grid-cols-[1fr_360px]">
                <div className="space-y-4">
                    {cart.items.map(
                        (
                            item,
                        ) => {
                            const canIncrease =
                                item.quantity <
                                item.availableStock;

                            const isUpdating =
                                setQuantity.isPending;

                            return (
                                <div
                                    key={
                                        item.productVariantId
                                    }
                                    className="rounded-2xl border bg-background p-5"
                                >
                                    <div className="flex flex-col gap-5 sm:flex-row">
                                        <div className="size-28 shrink-0 overflow-hidden rounded-xl bg-muted">
                                            {item.imageUrl ? (
                                                <img
                                                    src={
                                                        item.imageUrl
                                                    }
                                                    alt={
                                                        item.productName
                                                    }
                                                    className="h-full w-full object-cover"
                                                />
                                            ) : (
                                                <div className="flex h-full items-center justify-center">
                                                    <ShoppingBag className="size-8 text-muted-foreground" />
                                                </div>
                                            )}
                                        </div>

                                        <div className="min-w-0 flex-1">
                                            <div className="flex flex-col gap-2 sm:flex-row sm:items-start sm:justify-between">
                                                <div>
                                                    <h2 className="font-semibold">
                                                        {
                                                            item.productName
                                                        }
                                                    </h2>

                                                    <div className="mt-1 text-sm text-muted-foreground">
                                                        SKU:{' '}
                                                        {
                                                            item.sku
                                                        }
                                                    </div>
                                                </div>

                                                <div className="font-semibold">
                                                    {formatMoney(
                                                        item.unitPrice,
                                                        cart.currency,
                                                    )}
                                                </div>
                                            </div>


                                            <div className="mt-4 flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
                                                <div className="flex items-center gap-3">
                                                    <div className="flex items-center rounded-xl border">
                                                        <button
                                                            type="button"
                                                            aria-label="Decrease quantity"
                                                            className="p-2"
                                                            disabled={
                                                                item.quantity <=
                                                                    1 ||
                                                                isUpdating
                                                            }
                                                            onClick={() =>
                                                                setQuantity.mutate(
                                                                    {
                                                                        productVariantId:
                                                                            item.productVariantId,

                                                                        quantity:
                                                                            item.quantity -
                                                                            1,
                                                                    },
                                                                )
                                                            }
                                                        >
                                                            <Minus className="size-4" />
                                                        </button>

                                                        <span className="min-w-10 text-center font-medium">
                                                            {
                                                                item.quantity
                                                            }
                                                        </span>

                                                        <button
                                                            type="button"
                                                            aria-label="Increase quantity"
                                                            className="p-2"
                                                            disabled={
                                                                !canIncrease ||
                                                                isUpdating
                                                            }
                                                            onClick={() =>
                                                                setQuantity.mutate(
                                                                    {
                                                                        productVariantId:
                                                                            item.productVariantId,

                                                                        quantity:
                                                                            item.quantity +
                                                                            1,
                                                                    },
                                                                )
                                                            }
                                                        >
                                                            <Plus className="size-4" />
                                                        </button>
                                                    </div>

                                                    <span className="text-xs text-muted-foreground">
                                                        Available:{' '}
                                                        {
                                                            item.availableStock
                                                        }
                                                    </span>
                                                </div>


                                                <button
                                                    type="button"
                                                    className="inline-flex items-center gap-1 text-sm font-medium text-destructive disabled:opacity-50"
                                                    disabled={
                                                        remove.isPending
                                                    }
                                                    onClick={() =>
                                                        remove.mutate(
                                                            item.productVariantId,
                                                        )
                                                    }
                                                >
                                                    <Trash2 className="size-4" />
                                                    Remove
                                                </button>
                                            </div>
                                        </div>


                                        <div className="self-end text-lg font-bold sm:self-start">
                                            {formatMoney(
                                                item.lineTotal,
                                                cart.currency,
                                            )}
                                        </div>
                                    </div>
                                </div>
                            );
                        },
                    )}
                </div>


                <aside className="h-fit rounded-2xl border bg-background p-6 lg:sticky lg:top-6">
                    <h2 className="text-xl font-semibold">
                        Order Summary
                    </h2>

                    <div className="mt-6 space-y-3">
                        <div className="flex justify-between">
                            <span>
                                Subtotal
                            </span>

                            <span>
                                {formatMoney(
                                    cart.subtotal,
                                    cart.currency,
                                )}
                            </span>
                        </div>

                        <div className="flex justify-between text-sm text-muted-foreground">
                            <span>
                                Shipping
                            </span>

                            <span>
                                Calculated at checkout
                            </span>
                        </div>
                    </div>


                    <div className="my-6 border-t" />


                    <div className="flex justify-between text-lg font-bold">
                        <span>
                            Total
                        </span>

                        <span>
                            {formatMoney(
                                cart.totalAmount,
                                cart.currency,
                            )}
                        </span>
                    </div>


                    {setQuantity.isError && (
                        <div className="mt-4 rounded-xl border border-destructive/30 bg-destructive/5 p-3 text-sm text-destructive">
                            {getErrorMessage(
                                setQuantity.error,
                            )}
                        </div>
                    )}


                    {remove.isError && (
                        <div className="mt-4 rounded-xl border border-destructive/30 bg-destructive/5 p-3 text-sm text-destructive">
                            {getErrorMessage(
                                remove.error,
                            )}
                        </div>
                    )}


                    {clear.isError && (
                        <div className="mt-4 rounded-xl border border-destructive/30 bg-destructive/5 p-3 text-sm text-destructive">
                            {getErrorMessage(
                                clear.error,
                            )}
                        </div>
                    )}


                    <Link
                        to={
                            canCheckout
                                ? '/checkout'
                                : '/products'
                        }
                        className={`mt - 6 flex w - full items - center justify - center rounded - xl px - 5 py - 3 font - semibold ${
    canCheckout
        ? 'border bg-primary text-primary-foreground'
        : 'border text-muted-foreground'
} `}
                    >
                        {canCheckout
                            ? 'Proceed to checkout'
                            : 'Continue shopping'}
                    </Link>


                    <Link
                        to="/products"
                        className="mt-3 flex w-full items-center justify-center rounded-xl border px-5 py-3 font-medium"
                    >
                        Continue shopping
                    </Link>
                </aside>
            </div>
        </div>
    );
}

