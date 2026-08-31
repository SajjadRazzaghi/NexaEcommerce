import { Link, useParams } from 'react-router-dom';

import { useOrder } from '@/modules/orders/hooks/useOrders';

export default function OrderDetailsPage() {
    const { id } = useParams();

    const {
        data: order,
        isLoading,
    } = useOrder(id);

    if (isLoading) {
        return (
            <div className="mx-auto max-w-6xl p-6">
                Loading order...
            </div>
        );
    }

    if (!order) {
        return (
            <div className="mx-auto max-w-6xl p-6">
                Order not found.
            </div>
        );
    }

    return (
        <div className="mx-auto max-w-6xl p-6">
            <div className="mb-8 flex flex-wrap items-center justify-between gap-4">
                <div>
                    <h1 className="text-3xl font-bold">
                        Order {order.orderNumber}
                    </h1>

                    <div className="mt-2 text-muted-foreground">
                        Status: {order.status}
                    </div>
                </div>

                <Link
                    to="/orders"
                    className="rounded-lg border px-4 py-2"
                >
                    Back to orders
                </Link>
            </div>

            <div className="grid gap-6 lg:grid-cols-[1fr_360px]">
                <div className="space-y-4">
                    {order.items.map((item) => (
                        <div
                            key={item.productVariantId}
                            className="rounded-2xl border p-5"
                        >
                            <div className="flex justify-between gap-5">
                                <div>
                                    <div className="font-semibold">
                                        {item.productName}
                                    </div>

                                    <div className="mt-1 text-sm text-muted-foreground">
                                        {item.sku}
                                    </div>

                                    <div className="mt-3">
                                        Quantity: {item.quantity}
                                    </div>
                                </div>

                                <div className="font-semibold">
                                    {item.lineTotal.toLocaleString()}{' '}
                                    {order.currency}
                                </div>
                            </div>
                        </div>
                    ))}
                </div>

                <aside className="h-fit rounded-2xl border p-6">
                    <h2 className="text-xl font-semibold">
                        Shipping
                    </h2>

                    <div className="mt-5 space-y-2 text-sm">
                        <div>{order.shippingFullName}</div>
                        <div>{order.shippingPhone}</div>
                        <div>{order.shippingAddress}</div>
                        <div>{order.shippingCity}</div>

                        {order.shippingPostalCode && (
                            <div>
                                {order.shippingPostalCode}
                            </div>
                        )}
                    </div>

                    <div className="my-6 border-t" />

                    <div className="flex justify-between">
                        <span>Subtotal</span>
                        <span>
                            {order.subtotal.toLocaleString()}
                        </span>
                    </div>

                    <div className="mt-3 flex justify-between">
                        <span>Shipping</span>
                        <span>
                            {order.shippingAmount.toLocaleString()}
                        </span>
                    </div>

                    <div className="mt-5 flex justify-between text-lg font-bold">
                        <span>Total</span>
                        <span>
                            {order.totalAmount.toLocaleString()}{' '}
                            {order.currency}
                        </span>
                    </div>
                </aside>
            </div>
        </div>
    );
}