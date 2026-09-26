import {
    useState,
} from 'react';

import {
    useMutation,
    useQueryClient,
} from '@tanstack/react-query';

import {
    Edit3,
    Plus,
    RefreshCw,
    Truck,
} from 'lucide-react';

import {
    useTranslation,
} from 'react-i18next';

import {
    useAuth,
} from '@/hooks/use-auth';

import {
    hasPermission,
} from '@/lib/permissions';

import {
    PERM,
} from '@/lib/api/admin';

import {
    createShippingMethod,
    setShippingMethodActive,
    updateShippingMethod,
    type CreateShippingMethodRequest,
    type ShippingMethod,
    type UpdateShippingMethodRequest,
} from '@/modules/orders/api/shippingMethodsApi';

import {
    adminShippingMethodsQueryKey,
    shippingMethodsQueryKey,
    useAdminShippingMethods,
} from '@/modules/orders/hooks/useShippingMethods';

type FormState = {
    code: string;
    name: string;
    carrier: string;
    price: string;
    sortOrder: string;
};

const emptyForm: FormState = {
    code: '',
    name: '',
    carrier: '',
    price: '0',
    sortOrder: '0',
};

function errorMessage(
    error: unknown,
): string {
    const value =
        error as {
            response?: {
                data?: {
                    error?: string;
                    message?: string;
                };
            };
            message?: string;
        };

    return (
        value.response?.data?.error ??
        value.response?.data?.message ??
        value.message ??
        ''
    );
}

export default function AdminShippingMethodsPage() {
    const {
        i18n,
    } =
        useTranslation();

    const isFa =
        i18n.language
            .toLowerCase()
            .startsWith('fa');

    const {
        user,
    } =
        useAuth();

    const granted =
        user?.permissions ?? [];

    const canCreate =
        hasPermission(
            granted,
            PERM.shippingCreate,
        );

    const canUpdate =
        hasPermission(
            granted,
            PERM.shippingUpdate,
        );

    const queryClient =
        useQueryClient();

    const {
        data = [],
        isLoading,
        isFetching,
        isError,
        error,
        refetch,
    } =
        useAdminShippingMethods();

    const [
        editingId,
        setEditingId,
    ] =
        useState<string | null>(
            null,
        );

    const [
        form,
        setForm,
    ] =
        useState<FormState>(
            emptyForm,
        );

    const [
        formError,
        setFormError,
    ] =
        useState<string | null>(
            null,
        );

    const text = isFa
        ? {
            title:
                'روش‌های ارسال',
            description:
                'تعریف روش ارسال، شرکت حمل و هزینه‌ای که در checkout استفاده می‌شود.',
            new:
                'روش ارسال جدید',
            edit:
                'ویرایش روش ارسال',
            code:
                'کد',
            name:
                'نام روش',
            carrier:
                'شرکت حمل',
            price:
                'هزینه',
            sortOrder:
                'ترتیب',
            active:
                'فعال',
            inactive:
                'غیرفعال',
            activate:
                'فعال‌سازی',
            deactivate:
                'غیرفعال‌سازی',
            save:
                'ذخیره',
            cancel:
                'لغو',
            editAction:
                'ویرایش',
            loading:
                'در حال بارگذاری...',
            refresh:
                'به‌روزرسانی',
            empty:
                'هنوز روش ارسالی تعریف نشده است.',
            createHint:
                'مثلاً ارسال عادی، ارسال سریع یا پست پیشتاز.',
            codeHint:
                'کد یکتا برای استفاده داخلی فروشگاه.',
            error:
                'دریافت روش‌های ارسال ناموفق بود.',
            saveError:
                'ذخیره روش ارسال انجام نشد.',
            required:
                'کد، نام و شرکت حمل الزامی هستند.',
            invalidPrice:
                'هزینه ارسال نامعتبر است.',
            invalidSort:
                'ترتیب نمایش نامعتبر است.',
        }
        : {
            title:
                'Shipping Methods',
            description:
                'Define shipping services, carriers and prices used by checkout.',
            new:
                'New shipping method',
            edit:
                'Edit shipping method',
            code:
                'Code',
            name:
                'Method name',
            carrier:
                'Carrier',
            price:
                'Price',
            sortOrder:
                'Order',
            active:
                'Active',
            inactive:
                'Inactive',
            activate:
                'Activate',
            deactivate:
                'Deactivate',
            save:
                'Save',
            cancel:
                'Cancel',
            editAction:
                'Edit',
            loading:
                'Loading...',
            refresh:
                'Refresh',
            empty:
                'No shipping methods have been defined yet.',
            createHint:
                'For example standard delivery, express delivery or postal delivery.',
            codeHint:
                'Unique internal code used by the store.',
            error:
                'We could not load shipping methods.',
            saveError:
                'We could not save the shipping method.',
            required:
                'Code, method name and carrier are required.',
            invalidPrice:
                'Shipping price is invalid.',
            invalidSort:
                'Sort order is invalid.',
        };

    const save =
        useMutation({
            mutationFn:
                async () => {
                    setFormError(
                        null,
                    );

                    const code =
                        form.code.trim();

                    const name =
                        form.name.trim();

                    const carrier =
                        form.carrier.trim();

                    const price =
                        Number(
                            form.price,
                        );

                    const sortOrder =
                        Number(
                            form.sortOrder,
                        );

                    if (
                        !editingId &&
                        !code
                    ) {
                        throw new Error(
                            text.required,
                        );
                    }

                    if (
                        !name ||
                        !carrier
                    ) {
                        throw new Error(
                            text.required,
                        );
                    }

                    if (
                        !Number.isFinite(
                            price,
                        ) ||
                        price < 0
                    ) {
                        throw new Error(
                            text.invalidPrice,
                        );
                    }

                    if (
                        !Number.isInteger(
                            sortOrder,
                        ) ||
                        sortOrder < 0
                    ) {
                        throw new Error(
                            text.invalidSort,
                        );
                    }

                    if (editingId) {
                        const request:
                            UpdateShippingMethodRequest = {
                            name,
                            carrier,
                            price,
                            sortOrder,
                        };

                        return updateShippingMethod(
                            editingId,
                            request,
                        );
                    }

                    const request:
                        CreateShippingMethodRequest = {
                        code,
                        name,
                        carrier,
                        price,
                        sortOrder,
                    };

                    return createShippingMethod(
                        request,
                    );
                },

            onSuccess:
                async () => {
                    await Promise.all([
                        queryClient.invalidateQueries({
                            queryKey:
                                adminShippingMethodsQueryKey,
                        }),

                        queryClient.invalidateQueries({
                            queryKey:
                                shippingMethodsQueryKey,
                        }),
                    ]);

                    setEditingId(
                        null,
                    );

                    setForm(
                        emptyForm,
                    );

                    setFormError(
                        null,
                    );
                },

            onError:
                error => {
                    setFormError(
                        errorMessage(
                            error,
                        ) ||
                        text.saveError,
                    );
                },
        });

    const toggle =
        useMutation({
            mutationFn:
                ({
                    id,
                    active,
                }: {
                    id: string;
                    active: boolean;
                }) =>
                    setShippingMethodActive(
                        id,
                        active,
                    ),

            onSuccess:
                async () => {
                    await Promise.all([
                        queryClient.invalidateQueries({
                            queryKey:
                                adminShippingMethodsQueryKey,
                        }),

                        queryClient.invalidateQueries({
                            queryKey:
                                shippingMethodsQueryKey,
                        }),
                    ]);
                },
        });

    const startCreate =
        () => {
            setEditingId(
                null,
            );

            setForm(
                emptyForm,
            );

            setFormError(
                null,
            );
        };

    const startEdit =
        (
            method: ShippingMethod,
        ) => {
            setEditingId(
                method.id,
            );

            setForm({
                code:
                    method.code,
                name:
                    method.name,
                carrier:
                    method.carrier,
                price:
                    String(
                        method.price,
                    ),
                sortOrder:
                    String(
                        method.sortOrder,
                    ),
            });

            setFormError(
                null,
            );
        };

    const cancelEdit =
        () => {
            setEditingId(
                null,
            );

            setForm(
                emptyForm,
            );

            setFormError(
                null,
            );
        };

    const saving =
        save.isPending;

    return (
        <div
            className="grid gap-6"
            dir={
                isFa
                    ? 'rtl'
                    : 'ltr'
            }
        >
            <header className="flex flex-col gap-4 lg:flex-row lg:items-end lg:justify-between">
                <div>
                    <h1 className="text-2xl font-semibold">
                        {
                            text.title
                        }
                    </h1>

                    <p className="mt-1 text-sm text-muted-foreground">
                        {
                            text.description
                        }
                    </p>
                </div>

                <div className="flex flex-wrap items-center gap-2">
                    <button
                        type="button"
                        onClick={() =>
                            void refetch()
                        }
                        disabled={
                            isFetching
                        }
                        className="inline-flex items-center gap-2 rounded-lg border px-4 py-2.5 text-sm font-medium disabled:opacity-50"
                    >
                        <RefreshCw
                            className={`size-4 ${isFetching
                                    ? 'animate-spin'
                                    : ''
                                }`}
                        />

                        {
                            text.refresh
                        }
                    </button>

                    {canCreate && (
                        <button
                            type="button"
                            onClick={
                                startCreate
                            }
                            className="inline-flex items-center gap-2 rounded-lg bg-primary px-4 py-2.5 text-sm font-medium text-primary-foreground"
                        >
                            <Plus className="size-4" />

                            {
                                text.new
                            }
                        </button>
                    )}
                </div>
            </header>

            {(canCreate ||
                editingId) && (
                    <section className="rounded-2xl border bg-card p-6">
                        <div className="flex items-center gap-2">
                            <Truck className="size-5" />

                            <h2 className="font-semibold">
                                {
                                    editingId
                                        ? text.edit
                                        : text.new
                                }
                            </h2>
                        </div>
                    <div className="mt-5 grid min-w-0 grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3 2xl:grid-cols-5">
                        <label className="grid min-w-0 grid-rows-[auto_auto_1.25rem] gap-2">
                            <span className="text-sm font-medium">
                                {text.code}
                            </span>

                            <input
                                value={form.code}
                                onChange={event =>
                                    setForm(current => ({
                                        ...current,
                                        code: event.target.value,
                                    }))
                                }
                                disabled={Boolean(editingId) || saving}
                                maxLength={64}
                                placeholder="STANDARD"
                                className="block min-w-0 w-full rounded-lg border bg-background px-3 py-2.5 text-sm outline-none transition focus:ring-2 focus:ring-ring disabled:opacity-60"
                            />

                            <span className="text-xs text-muted-foreground">
                                {text.codeHint}
                            </span>
                        </label>

                        <label className="grid min-w-0 grid-rows-[auto_auto_1.25rem] gap-2">
                            <span className="text-sm font-medium">
                                {text.name}
                            </span>

                            <input
                                value={form.name}
                                onChange={event =>
                                    setForm(current => ({
                                        ...current,
                                        name: event.target.value,
                                    }))
                                }
                                disabled={saving}
                                maxLength={100}
                                placeholder="Standard delivery"
                                className="block min-w-0 w-full rounded-lg border bg-background px-3 py-2.5 text-sm outline-none transition focus:ring-2 focus:ring-ring disabled:opacity-60"
                            />

                            <span />
                        </label>

                        <label className="grid min-w-0 grid-rows-[auto_auto_1.25rem] gap-2">
                            <span className="text-sm font-medium">
                                {text.carrier}
                            </span>

                            <input
                                value={form.carrier}
                                onChange={event =>
                                    setForm(current => ({
                                        ...current,
                                        carrier: event.target.value,
                                    }))
                                }
                                disabled={saving}
                                maxLength={100}
                                placeholder="Postal service"
                                className="block min-w-0 w-full rounded-lg border bg-background px-3 py-2.5 text-sm outline-none transition focus:ring-2 focus:ring-ring disabled:opacity-60"
                            />

                            <span />
                        </label>

                        <label className="grid min-w-0 grid-rows-[auto_auto_1.25rem] gap-2">
                            <span className="text-sm font-medium">
                                {text.price}
                            </span>

                            <input
                                type="number"
                                min="0"
                                step="1"
                                value={form.price}
                                onChange={event =>
                                    setForm(current => ({
                                        ...current,
                                        price: event.target.value,
                                    }))
                                }
                                disabled={saving}
                                className="block min-w-0 w-full rounded-lg border bg-background px-3 py-2.5 text-sm outline-none transition focus:ring-2 focus:ring-ring disabled:opacity-60"
                            />

                            <span />
                        </label>

                        <label className="grid min-w-0 grid-rows-[auto_auto_1.25rem] gap-2">
                            <span className="text-sm font-medium">
                                {text.sortOrder}
                            </span>

                            <input
                                type="number"
                                min="0"
                                step="1"
                                value={form.sortOrder}
                                onChange={event =>
                                    setForm(current => ({
                                        ...current,
                                        sortOrder: event.target.value,
                                    }))
                                }
                                disabled={saving}
                                className="block min-w-0 w-full rounded-lg border bg-background px-3 py-2.5 text-sm outline-none transition focus:ring-2 focus:ring-ring disabled:opacity-60"
                            />

                            <span />
                        </label>
                    </div>
                 

                        <p className="mt-4 text-xs text-muted-foreground">
                            {
                                text.createHint
                            }
                        </p>

                        {formError && (
                            <div className="mt-4 rounded-lg border border-destructive/30 bg-destructive/5 p-3 text-sm text-destructive">
                                {
                                    formError
                                }
                            </div>
                        )}

                        <div className="mt-5 flex flex-wrap gap-2">
                            <button
                                type="button"
                                disabled={
                                    saving
                                }
                                onClick={() =>
                                    save.mutate()
                                }
                                className="rounded-lg bg-primary px-4 py-2.5 text-sm font-medium text-primary-foreground disabled:opacity-50"
                            >
                                {
                                    saving
                                        ? text.loading
                                        : text.save
                                }
                            </button>

                            <button
                                type="button"
                                disabled={
                                    saving
                                }
                                onClick={
                                    cancelEdit
                                }
                                className="rounded-lg border px-4 py-2.5 text-sm font-medium disabled:opacity-50"
                            >
                                {
                                    text.cancel
                                }
                            </button>
                        </div>
                    </section>
                )}

            {isLoading && (
                <div className="grid gap-3">
                    {[1, 2, 3].map(
                        item => (
                            <div
                                key={
                                    item
                                }
                                className="animate-pulse rounded-xl border p-5"
                            >
                                <div className="h-5 w-44 rounded bg-muted" />
                                <div className="mt-3 h-4 w-72 rounded bg-muted" />
                            </div>
                        ),
                    )}
                </div>
            )}

            {isError && (
                <div className="rounded-2xl border border-destructive/30 p-10 text-center">
                    <p className="text-sm text-destructive">
                        {errorMessage(
                            error,
                        ) ||
                            text.error}
                    </p>

                    <button
                        type="button"
                        onClick={() =>
                            void refetch()
                        }
                        className="mt-4 rounded-lg border px-4 py-2 text-sm"
                    >
                        {
                            text.refresh
                        }
                    </button>
                </div>
            )}

            {!isLoading &&
                !isError &&
                data.length === 0 && (
                    <div className="rounded-2xl border border-dashed p-12 text-center">
                        <Truck className="mx-auto size-10 text-muted-foreground" />

                        <h2 className="mt-4 font-semibold">
                            {
                                text.empty
                            }
                        </h2>
                    </div>
                )}

            {!isLoading &&
                !isError &&
                data.length > 0 && (
                    <section className="overflow-x-auto rounded-2xl border">
                        <table className="w-full min-w-[900px] text-sm">
                            <thead>
                                <tr className="border-b bg-muted/30">
                                    <th className="px-4 py-3 text-start font-medium">
                                        {
                                            text.code
                                        }
                                    </th>

                                    <th className="px-4 py-3 text-start font-medium">
                                        {
                                            text.name
                                        }
                                    </th>

                                    <th className="px-4 py-3 text-start font-medium">
                                        {
                                            text.carrier
                                        }
                                    </th>

                                    <th className="px-4 py-3 text-start font-medium">
                                        {
                                            text.price
                                        }
                                    </th>

                                    <th className="px-4 py-3 text-start font-medium">
                                        {
                                            text.active
                                        }
                                    </th>

                                    <th className="px-4 py-3 text-end font-medium">
                                        {text.editAction}
                                    </th>
                                </tr>
                            </thead>

                            <tbody>
                                {data.map(
                                    method => (
                                        <tr
                                            key={
                                                method.id
                                            }
                                            className="border-b last:border-b-0"
                                        >
                                            <td className="px-4 py-4 font-mono text-xs">
                                                {
                                                    method.code
                                                }
                                            </td>

                                            <td className="px-4 py-4 font-medium">
                                                {
                                                    method.name
                                                }
                                            </td>

                                            <td className="px-4 py-4">
                                                {
                                                    method.carrier
                                                }
                                            </td>

                                            <td className="px-4 py-4">
                                                {method.price.toLocaleString(
                                                    isFa
                                                        ? 'fa-IR'
                                                        : undefined,
                                                )}
                                            </td>

                                            <td className="px-4 py-4">
                                                <span
                                                    className={`inline-flex rounded-full border px-2.5 py-1 text-xs font-medium ${method.isActive
                                                            ? 'border-emerald-500/40 text-emerald-600 dark:text-emerald-400'
                                                            : 'border-border text-muted-foreground'
                                                        }`}
                                                >
                                                    {
                                                        method.isActive
                                                            ? text.active
                                                            : text.inactive
                                                    }
                                                </span>
                                            </td>

                                            <td className="px-4 py-4">
                                                <div className="flex justify-end gap-2">
                                                    {canUpdate && (
                                                        <button
                                                            type="button"
                                                            onClick={() =>
                                                                startEdit(
                                                                    method,
                                                                )
                                                            }
                                                            className="inline-flex items-center gap-1.5 rounded-lg border px-3 py-2 text-xs font-medium"
                                                        >
                                                            <Edit3 className="size-3.5" />

                                                            {
                                                                text.editAction
                                                            }
                                                        </button>
                                                    )}

                                                    {canUpdate && (
                                                        <button
                                                            type="button"
                                                            disabled={
                                                                toggle.isPending
                                                            }
                                                            onClick={() =>
                                                                toggle.mutate({
                                                                    id:
                                                                        method.id,
                                                                    active:
                                                                        !method.isActive,
                                                                })
                                                            }
                                                            className="rounded-lg border px-3 py-2 text-xs font-medium disabled:opacity-50"
                                                        >
                                                            {
                                                                method.isActive
                                                                    ? text.deactivate
                                                                    : text.activate
                                                            }
                                                        </button>
                                                    )}
                                                </div>
                                            </td>
                                        </tr>
                                    ),
                                )}
                            </tbody>
                        </table>
                    </section>
                )}
        </div>
    );
}