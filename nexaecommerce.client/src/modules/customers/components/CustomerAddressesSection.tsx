import {
    useMemo,
    useState,
} from 'react';

import type {
    FormEvent,
} from 'react';

import {
    useTranslation,
} from 'react-i18next';

import {
    Pencil,
    Plus,
    Star,
    Trash2,
} from 'lucide-react';

import {
    useCustomerAddresses,
    useCustomerAddressMutations,
} from '../hooks/useCustomerAddresses';

import type {
    CreateAddressRequest,
    CustomerAddress,
} from '../types';

interface AddressFormState {
    title: string;
    recipientName: string;
    phoneNumber: string;
    country: string;
    province: string;
    city: string;
    addressLine: string;
    postalCode: string;
    isDefault: boolean;
}

const emptyForm: AddressFormState = {
    title: '',
    recipientName: '',
    phoneNumber: '',
    country: '',
    province: '',
    city: '',
    addressLine: '',
    postalCode: '',
    isDefault: false,
};

export function CustomerAddressesSection() {
    const { i18n } = useTranslation();

    const isFa =
        i18n.language
            .toLowerCase()
            .startsWith('fa');

    const text = isFa
        ? {
            title: 'آدرس‌های من',
            description:
                'آدرس‌های ارسال خود را مدیریت کنید.',
            add: 'افزودن آدرس',
            edit: 'ویرایش',
            delete: 'حذف',
            default: 'پیش‌فرض',
            makeDefault:
                'انتخاب به عنوان پیش‌فرض',
            cancel: 'انصراف',
            save: 'ذخیره',
            update: 'به‌روزرسانی',
            empty:
                'هنوز آدرسی ثبت نکرده‌اید.',
            titleField: 'عنوان',
            recipient:
                'نام گیرنده',
            phone: 'شماره تلفن',
            country: 'کشور',
            province: 'استان',
            city: 'شهر',
            address:
                'آدرس کامل',
            postalCode:
                'کد پستی',
            defaultAddress:
                'این آدرس پیش‌فرض باشد',
            loading:
                'در حال دریافت آدرس‌ها...',
            saving:
                'در حال ذخیره...',
            deleting:
                'در حال حذف...',
            deleteConfirm:
                'آیا از حذف این آدرس مطمئن هستید؟',
            error:
                'عملیات با خطا مواجه شد.',
        }
        : {
            title: 'My addresses',
            description:
                'Manage your saved shipping addresses.',
            add: 'Add address',
            edit: 'Edit',
            delete: 'Delete',
            default: 'Default',
            makeDefault:
                'Make default',
            cancel: 'Cancel',
            save: 'Save',
            update: 'Update',
            empty:
                'You have no saved addresses yet.',
            titleField: 'Title',
            recipient:
                'Recipient name',
            phone: 'Phone number',
            country: 'Country',
            province: 'Province',
            city: 'City',
            address:
                'Full address',
            postalCode:
                'Postal code',
            defaultAddress:
                'Set as default address',
            loading:
                'Loading addresses...',
            saving:
                'Saving...',
            deleting:
                'Deleting...',
            deleteConfirm:
                'Are you sure you want to delete this address?',
            error:
                'The operation failed.',
        };

    const {
        data: addresses,
        isLoading,
        isError,
    } =
        useCustomerAddresses();

    const {
        create,
        update,
        remove,
        setDefault,
    } =
        useCustomerAddressMutations();

    const [
        editingAddress,
        setEditingAddress,
    ] =
        useState<
            CustomerAddress | null
        >(null);

    const [
        form,
        setForm,
    ] =
        useState<AddressFormState>(
            emptyForm,
        );

    const [
        formOpen,
        setFormOpen,
    ] =
        useState(false);

    const sortedAddresses =
        useMemo(
            () =>
                [...(addresses ?? [])].sort(
                    (a, b) => {
                        if (
                            a.isDefault !==
                            b.isDefault
                        ) {
                            return a.isDefault
                                ? -1
                                : 1;
                        }

                        return a.title.localeCompare(
                            b.title,
                        );
                    },
                ),
            [addresses],
        );

    function openCreate() {
        setEditingAddress(null);

        setForm({
            ...emptyForm,
            isDefault:
                !addresses ||
                addresses.length === 0,
        });

        setFormOpen(true);
    }

    function openEdit(
        address: CustomerAddress,
    ) {
        setEditingAddress(address);

        setForm({
            title: address.title,
            recipientName:
                address.recipientName,
            phoneNumber:
                address.phoneNumber,
            country:
                address.country,
            province:
                address.province,
            city:
                address.city,
            addressLine:
                address.addressLine,
            postalCode:
                address.postalCode ?? '',
            isDefault:
                address.isDefault,
        });

        setFormOpen(true);
    }

    function closeForm() {
        setFormOpen(false);
        setEditingAddress(null);
        setForm(emptyForm);
    }

    function updateField(
        field: keyof AddressFormState,
        value: string | boolean,
    ) {
        setForm(
            current => ({
                ...current,
                [field]: value,
            }),
        );
    }

    async function submit(
        event: FormEvent,
    ) {
        event.preventDefault();

        const request: CreateAddressRequest =
        {
            title:
                form.title.trim(),
            recipientName:
                form.recipientName.trim(),
            phoneNumber:
                form.phoneNumber.trim(),
            country:
                form.country.trim(),
            province:
                form.province.trim(),
            city:
                form.city.trim(),
            addressLine:
                form.addressLine.trim(),
            postalCode:
                form.postalCode.trim() ||
                null,
            isDefault:
                form.isDefault,
        };

        if (
            editingAddress
        ) {
            await update.mutateAsync({
                id: editingAddress.id,
                request,
            });

            closeForm();
            return;
        }

        await create.mutateAsync(
            request,
        );

        closeForm();
    }

    async function handleDelete(
        id: string,
    ) {
        if (
            !window.confirm(
                text.deleteConfirm,
            )
        ) {
            return;
        }

        await remove.mutateAsync(id);
    }

    const mutationError =
        create.error ??
        update.error ??
        remove.error ??
        setDefault.error;

    return (
        <section
            className="grid gap-5"
            dir={
                isFa
                    ? 'rtl'
                    : 'ltr'
            }
        >
            <div className="flex flex-wrap items-center justify-between gap-3">
                <div>
                    <h2 className="text-lg font-semibold">
                        {text.title}
                    </h2>

                    <p className="text-muted-foreground mt-1 text-sm">
                        {text.description}
                    </p>
                </div>

                <button
                    type="button"
                    onClick={openCreate}
                    className="bg-primary text-primary-foreground inline-flex items-center gap-2 rounded-lg px-4 py-2 text-sm font-medium"
                >
                    <Plus className="size-4" />
                    {text.add}
                </button>
            </div>

            {isLoading && (
                <div className="rounded-xl border p-5 text-sm">
                    {text.loading}
                </div>
            )}

            {isError && (
                <div className="rounded-xl border border-destructive/40 p-5 text-sm text-destructive">
                    {text.error}
                </div>
            )}

            {!isLoading &&
                !isError &&
                sortedAddresses.length ===
                0 && (
                    <div className="rounded-xl border border-dashed p-8 text-center">
                        <p className="text-muted-foreground text-sm">
                            {text.empty}
                        </p>

                        <button
                            type="button"
                            onClick={
                                openCreate
                            }
                            className="mt-4 rounded-lg border px-4 py-2 text-sm font-medium"
                        >
                            {text.add}
                        </button>
                    </div>
                )}

            {sortedAddresses.length >
                0 && (
                    <div className="grid gap-4">
                        {sortedAddresses.map(
                            address => (
                                <article
                                    key={
                                        address.id
                                    }
                                    className={`rounded-xl border p-5 ${address.isDefault
                                            ? 'border-primary/50'
                                            : ''
                                        }`}
                                >
                                    <div className="flex flex-wrap items-start justify-between gap-4">
                                        <div className="min-w-0">
                                            <div className="flex flex-wrap items-center gap-2">
                                                <h3 className="font-semibold">
                                                    {
                                                        address.title
                                                    }
                                                </h3>

                                                {address.isDefault && (
                                                    <span className="inline-flex items-center gap-1 rounded-full border px-2 py-0.5 text-xs">
                                                        <Star className="size-3 fill-current" />
                                                        {
                                                            text.default
                                                        }
                                                    </span>
                                                )}
                                            </div>

                                            <p className="mt-3 font-medium">
                                                {
                                                    address.recipientName
                                                }
                                            </p>

                                            <p className="text-muted-foreground mt-1 text-sm">
                                                {
                                                    address.phoneNumber
                                                }
                                            </p>

                                            <p className="text-muted-foreground mt-3 text-sm leading-6">
                                                {
                                                    address.addressLine
                                                }
                                            </p>

                                            <p className="text-muted-foreground text-sm">
                                                {[
                                                    address.city,
                                                    address.province,
                                                    address.country,
                                                ]
                                                    .filter(
                                                        Boolean,
                                                    )
                                                    .join(
                                                        '، ',
                                                    )}
                                            </p>

                                            {address.postalCode && (
                                                <p className="text-muted-foreground mt-1 text-sm">
                                                    {
                                                        address.postalCode
                                                    }
                                                </p>
                                            )}
                                        </div>

                                        <div className="flex flex-wrap gap-2">
                                            {!address.isDefault && (
                                                <button
                                                    type="button"
                                                    disabled={
                                                        setDefault.isPending
                                                    }
                                                    onClick={() =>
                                                        setDefault.mutate(
                                                            address.id,
                                                        )
                                                    }
                                                    className="rounded-lg border px-3 py-2 text-xs font-medium"
                                                >
                                                    {
                                                        text.makeDefault
                                                    }
                                                </button>
                                            )}

                                            <button
                                                type="button"
                                                onClick={() =>
                                                    openEdit(
                                                        address,
                                                    )
                                                }
                                                className="inline-flex items-center gap-1 rounded-lg border px-3 py-2 text-xs font-medium"
                                            >
                                                <Pencil className="size-3.5" />
                                                {
                                                    text.edit
                                                }
                                            </button>

                                            <button
                                                type="button"
                                                disabled={
                                                    remove.isPending
                                                }
                                                onClick={() =>
                                                    void handleDelete(
                                                        address.id,
                                                    )
                                                }
                                                className="text-destructive inline-flex items-center gap-1 rounded-lg border px-3 py-2 text-xs font-medium"
                                            >
                                                <Trash2 className="size-3.5" />
                                                {
                                                    remove.isPending
                                                        ? text.deleting
                                                        : text.delete
                                                }
                                            </button>
                                        </div>
                                    </div>
                                </article>
                            ),
                        )}
                    </div>
                )}

            {formOpen && (
                <div className="rounded-xl border bg-card p-5">
                    <div className="mb-5">
                        <h3 className="font-semibold">
                            {editingAddress
                                ? text.edit
                                : text.add}
                        </h3>
                    </div>

                    <form
                        onSubmit={
                            submit
                        }
                        className="grid gap-4"
                    >
                        <div className="grid gap-4 md:grid-cols-2">
                            <Field
                                label={
                                    text.titleField
                                }
                                value={
                                    form.title
                                }
                                required
                                onChange={value =>
                                    updateField(
                                        'title',
                                        value,
                                    )
                                }
                            />

                            <Field
                                label={
                                    text.recipient
                                }
                                value={
                                    form.recipientName
                                }
                                required
                                onChange={value =>
                                    updateField(
                                        'recipientName',
                                        value,
                                    )
                                }
                            />

                            <Field
                                label={
                                    text.phone
                                }
                                value={
                                    form.phoneNumber
                                }
                                required
                                type="tel"
                                onChange={value =>
                                    updateField(
                                        'phoneNumber',
                                        value,
                                    )
                                }
                            />

                            <Field
                                label={
                                    text.country
                                }
                                value={
                                    form.country
                                }
                                required
                                onChange={value =>
                                    updateField(
                                        'country',
                                        value,
                                    )
                                }
                            />

                            <Field
                                label={
                                    text.province
                                }
                                value={
                                    form.province
                                }
                                required
                                onChange={value =>
                                    updateField(
                                        'province',
                                        value,
                                    )
                                }
                            />

                            <Field
                                label={
                                    text.city
                                }
                                value={
                                    form.city
                                }
                                required
                                onChange={value =>
                                    updateField(
                                        'city',
                                        value,
                                    )
                                }
                            />

                            <Field
                                label={
                                    text.postalCode
                                }
                                value={
                                    form.postalCode
                                }
                                onChange={value =>
                                    updateField(
                                        'postalCode',
                                        value,
                                    )
                                }
                            />
                        </div>

                        <label className="grid gap-2">
                            <span className="text-sm font-medium">
                                {
                                    text.address
                                }
                            </span>

                            <textarea
                                required
                                rows={4}
                                value={
                                    form.addressLine
                                }
                                onChange={event =>
                                    updateField(
                                        'addressLine',
                                        event
                                            .target
                                            .value,
                                    )
                                }
                                className="rounded-lg border bg-background px-3 py-2 text-sm outline-none focus:ring-2"
                            />
                        </label>

                        <label className="flex items-center gap-2 text-sm">
                            <input
                                type="checkbox"
                                checked={
                                    form.isDefault
                                }
                                onChange={event =>
                                    updateField(
                                        'isDefault',
                                        event
                                            .target
                                            .checked,
                                    )
                                }
                            />

                            {
                                text.defaultAddress
                            }
                        </label>

                        {mutationError && (
                            <div className="rounded-lg border border-destructive/40 p-3 text-sm text-destructive">
                                {text.error}
                            </div>
                        )}

                        <div className="flex flex-wrap gap-2">
                            <button
                                type="button"
                                onClick={
                                    closeForm
                                }
                                className="rounded-lg border px-4 py-2 text-sm font-medium"
                            >
                                {
                                    text.cancel
                                }
                            </button>

                            <button
                                type="submit"
                                disabled={
                                    create.isPending ||
                                    update.isPending
                                }
                                className="bg-primary text-primary-foreground rounded-lg px-4 py-2 text-sm font-medium"
                            >
                                {create.isPending ||
                                    update.isPending
                                    ? text.saving
                                    : editingAddress
                                        ? text.update
                                        : text.save}
                            </button>
                        </div>
                    </form>
                </div>
            )}
        </section>
    );
}

interface FieldProps {
    label: string;
    value: string;
    required?: boolean;
    type?: string;
    onChange: (
        value: string,
    ) => void;
}

function Field({
    label,
    value,
    required,
    type = 'text',
    onChange,
}: FieldProps) {
    return (
        <label className="grid gap-2">
            <span className="text-sm font-medium">
                {label}
            </span>

            <input
                type={type}
                required={required}
                value={value}
                onChange={event =>
                    onChange(
                        event.target.value,
                    )
                }
                className="rounded-lg border bg-background px-3 py-2 text-sm outline-none focus:ring-2"
            />
        </label>
    );
}