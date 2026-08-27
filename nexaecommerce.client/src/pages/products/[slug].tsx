import { useParams } from 'react-router-dom';
import { Link } from 'react-router-dom';
import { useProductBySlug } from '@/modules/catalog/products/hooks';
import { formatPrice } from '@/lib/utils';

export default function ProductPage() {
    const { slug } = useParams<{ slug: string }>();

    const {
        data: product,
        isLoading,
        isError,
    } = useProductBySlug(slug);

    if (isLoading) {
        return (
            <div className="container mx-auto px-4 py-10">
                Loading product...
            </div>
        );
    }

    if (isError || !product) {
        return (
            <div className="container mx-auto px-4 py-10">
                <h1 className="text-2xl font-semibold">
                    Product not found
                </h1>

                <Link
                    to="/"
                    className="mt-4 inline-block underline"
                >
                    Back to store
                </Link>
            </div>
        );
    }

    const primaryImage =
        product.images?.find(image => image.isMain) ??
        product.images?.[0];

    return (
        <main className="container mx-auto px-4 py-8">
            <div className="grid gap-8 lg:grid-cols-2">
                <div className="rounded-xl border bg-background p-4">
                    {primaryImage ? (
                        <img
                            src={primaryImage.imageUrl}
                            alt={
                                primaryImage.altText ??
                                product.name
                            }
                            className="aspect-square w-full rounded-lg object-contain"
                        />
                    ) : (
                        <div className="grid aspect-square place-items-center text-muted-foreground">
                            No image
                        </div>
                    )}
                </div>

                <div className="space-y-6">
                    <div>
                        <div className="mb-2 text-sm text-muted-foreground">
                            {product.brandName}
                        </div>

                        <h1 className="text-3xl font-bold">
                            {product.name}
                        </h1>

                        {product.shortDescription && (
                            <p className="mt-3 text-muted-foreground">
                                {product.shortDescription}
                            </p>
                        )}
                    </div>

                    <div className="flex items-baseline gap-3">
                        <span className="text-3xl font-bold">
                            {formatPrice(product.finalPrice)}
                        </span>

                        {product.price !== product.finalPrice && (
                            <span className="text-muted-foreground line-through">
                                {formatPrice(product.price)}
                            </span>
                        )}
                    </div>

                    {product.discountPercentage > 0 && (
                        <div className="text-sm font-medium">
                            {product.discountPercentage}% OFF
                        </div>
                    )}

                    <div className="text-sm">
                        {product.isInStock
                            ? `In stock (${product.stockQuantity})`
                            : 'Out of stock'}
                    </div>

                    {product.description && (
                        <section>
                            <h2 className="mb-2 text-lg font-semibold">
                                Description
                            </h2>

                            <p className="whitespace-pre-wrap text-muted-foreground">
                                {product.description}
                            </p>
                        </section>
                    )}

                    {product.variants?.length > 0 && (
                        <section>
                            <h2 className="mb-3 text-lg font-semibold">
                                Variants
                            </h2>

                            <div className="grid gap-3 sm:grid-cols-2">
                                {product.variants
                                    .filter(v => v.isActive)
                                    .map(variant => (
                                        <div
                                            key={variant.id}
                                            className="rounded-lg border p-4"
                                        >
                                            <div className="font-medium">
                                                {variant.color ||
                                                    variant.size ||
                                                    variant.sku}
                                            </div>

                                            <div className="mt-1 text-sm text-muted-foreground">
                                                SKU: {variant.sku}
                                            </div>

                                            <div className="mt-2">
                                                {variant.stockQuantity > 0
                                                    ? `In stock: ${variant.stockQuantity}`
                                                    : 'Out of stock'}
                                            </div>
                                        </div>
                                    ))}
                            </div>
                        </section>
                    )}
                </div>
            </div>
        </main>
    );
}