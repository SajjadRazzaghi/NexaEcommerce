import {
    type FormEvent,
    useState,
} from 'react';

import {
    Link,
    useNavigate,
} from 'react-router-dom';

import {
    Search,
    ShoppingBag,
} from 'lucide-react';

export default function StoreHeader() {
    const navigate = useNavigate();
    const [query, setQuery] = useState('');

    const submitSearch = (
        event: FormEvent<HTMLFormElement>,
    ) => {
        event.preventDefault();

        const value = query.trim();

        if (!value) {
            navigate('/products');
            return;
        }

        navigate(
            `/products?search=${encodeURIComponent(value)}`,
        );
    };

    return (
        <header className="sticky top-0 z-40 border-b bg-background/95 backdrop-blur">
            <div className="mx-auto flex max-w-7xl items-center gap-3 px-4 py-3 sm:px-6">
                <Link
                    to="/"
                    className="flex shrink-0 items-center gap-2"
                >
                    <div className="flex size-10 items-center justify-center rounded-xl bg-primary text-primary-foreground">
                        <ShoppingBag className="size-5" />
                    </div>

                    <div className="hidden sm:block">
                        <div className="text-lg font-black tracking-tight">
                            NexaECommerce
                        </div>

                        <div className="text-[11px] text-muted-foreground">
                            Online Store
                        </div>
                    </div>
                </Link>

                <form
                    onSubmit={submitSearch}
                    className="mx-auto flex min-w-0 flex-1"
                >
                    <div className="flex w-full items-center rounded-2xl border bg-muted/40 px-3 transition-colors focus-within:border-primary">
                        <Search className="size-5 shrink-0 text-muted-foreground" />

                        <input
                            value={query}
                            onChange={(event) =>
                                setQuery(event.target.value)
                            }
                            placeholder="Search products..."
                            className="h-11 w-full bg-transparent px-3 text-sm outline-none"
                            aria-label="Search products"
                        />

                        <button
                            type="submit"
                            className="hidden rounded-xl bg-primary px-4 py-2 text-sm font-bold text-primary-foreground sm:block"
                        >
                            Search
                        </button>
                    </div>
                </form>

                <Link
                    to="/products"
                    className="hidden rounded-xl border px-4 py-2 text-sm font-semibold transition-colors hover:bg-muted lg:block"
                >
                    Products
                </Link>

                <Link
                    to="/cart"
                    className="flex size-11 shrink-0 items-center justify-center rounded-xl border transition-colors hover:bg-muted"
                    aria-label="Shopping cart"
                    title="Shopping cart"
                >
                    <ShoppingBag className="size-5" />
                </Link>

                <Link
                    to="/login"
                    className="hidden rounded-xl bg-primary px-4 py-2 text-sm font-bold text-primary-foreground transition-opacity hover:opacity-90 md:block"
                >
                    Sign in
                </Link>
            </div>
        </header>
    );
}