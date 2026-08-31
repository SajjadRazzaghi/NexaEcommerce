import { Link } from 'react-router-dom';

import { useOrders } from '@/modules/orders/hooks/useOrders';

function statusClass(status: string) {
    switch (status) {
        case 'Paid':
            return 'border';
        case 'Processing':
            return 'border';
        case 'Shipped':
            return 'border';
        case 'Delivered':
            return 'border';
        case 'Cancelled':
            return 'border text-destructive';
        default:
            return 'border';
    }
}

export default function OrdersPage() {
    const {
        data,
        isLoading,
    } = useOrders();

    if (isLoading) {
        return (
            <div className="mx-auto max-w-6xl p-6">
                Loading orders...
            </div>
        );
    }

    return (
        <div className="mx-auto max-w-6xl p-6">
            <div className="mb-8">
                <h1 className="text-3xl font-bold">
                    My Orders
                </h1>
            </div>

            {!data || data.items.length === 0 ? (
                <div className="rounded-2xl border p-12 text-center">
                    <h2 className="text-xl font-semibold">
                        No orders yet
                    </h2>

                    <Link
                        to="/products"
                        className="mt-5 inline-flex rounded-lg border px-5 py-3"
                    >
                        Start shopping
                    </Link>
                </div>
            ) : (
                <div className="space-y-4">
                    {data.items.map((order) => (
                        <Link
                            key={order.id}
                            to={`/orders/${order.id}`}
                            className="block rounded-2xl border p-5 transition hover:shadow-sm"
                        >
                            <div className="flex flex-wrap items-center justify-between gap-4">
                                <div>
                                    <div className="font-semibold">
                                        {order.orderNumber}
                                    </div>

                                    <div className="mt-1 text-sm text-muted-foreground">
                                        {new Date(
                                            order.createdAt,
                                        ).toLocaleString()}
                                    </div>
                                </div>

                                <div
                                    className={`rounded-full px-3 py-1 text-sm ${statusClass(
                                        order.status,
                                    )}`}
                                >
                                    {order.status}
                                </div>

                                <div className="font-bold">
                                    {order.totalAmount.toLocaleString()}{' '}
                                    {order.currency}
                                </div>
                            </div>
                        </Link>
                    ))}
                </div>
            )}
        </div>
    );
}