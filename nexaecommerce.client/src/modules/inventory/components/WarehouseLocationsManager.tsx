import { useMemo, useState, type FormEvent } from 'react';
import { useTranslation } from 'react-i18next';
import { Pencil, Power, PowerOff, Plus, X } from 'lucide-react';
import { toast } from 'sonner';

import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Badge } from '@/components/ui/badge';
import {
    useCreateWarehouseLocation,
    useSetWarehouseLocationStatus,
    useUpdateWarehouseLocation,
    useWarehouseLocations,
} from '@/modules/inventory/hooks/useWarehouses';
import type { WarehouseLocation } from '@/lib/api/inventory';

export default function WarehouseLocationsManager({ warehouseId }: { warehouseId: string }) {
    const { t } = useTranslation();
    const query = useWarehouseLocations(warehouseId, true);
    const createMutation = useCreateWarehouseLocation();
    const updateMutation = useUpdateWarehouseLocation();
    const statusMutation = useSetWarehouseLocationStatus();

    const [editing, setEditing] = useState<WarehouseLocation | null>(null);
    const [showForm, setShowForm] = useState(false);
    const [form, setForm] = useState({
        code: '',
        name: '',
        zone: '',
        rack: '',
        shelf: '',
        bin: '',
    });

    const activeCount = useMemo(
        () => (query.data ?? []).filter((item) => item.isActive).length,
        [query.data],
    );

    const openCreate = () => {
        setEditing(null);
        setForm({ code: '', name: '', zone: '', rack: '', shelf: '', bin: '' });
        setShowForm(true);
    };

    const openEdit = (location: WarehouseLocation) => {
        setEditing(location);
        setForm({
            code: location.code,
            name: location.name,
            zone: location.zone ?? '',
            rack: location.rack ?? '',
            shelf: location.shelf ?? '',
            bin: location.bin ?? '',
        });
        setShowForm(true);
    };

    const closeForm = () => {
        setEditing(null);
        setShowForm(false);
    };

    const submit = async (event: FormEvent<HTMLFormElement>) => {
        event.preventDefault();
        const body = {
            code: form.code.trim().toUpperCase(),
            name: form.name.trim(),
            zone: form.zone.trim() || null,
            rack: form.rack.trim() || null,
            shelf: form.shelf.trim() || null,
            bin: form.bin.trim() || null,
        };

        if (!body.code || !body.name) {
            toast.error(t('inventory.locationRequired'));
            return;
        }

        try {
            if (editing) {
                await updateMutation.mutateAsync({
                    warehouseId,
                    locationId: editing.id,
                    body,
                });
                toast.success(t('inventory.locationUpdated'));
            } else {
                await createMutation.mutateAsync({
                    warehouseId,
                    ...body,
                });
                toast.success(t('inventory.locationCreated'));
            }
            closeForm();
        } catch (error) {
            toast.error(error instanceof Error ? error.message : t('inventory.locationSaveError'));
        }
    };

    return (
        <Card>
            <CardHeader>
                <div className="flex flex-col gap-3 md:flex-row md:items-start md:justify-between">
                    <div>
                        <CardTitle>{t('inventory.locationsTitle')}</CardTitle>
                        <CardDescription>
                            {t('inventory.locationsDescription', { count: activeCount })}
                        </CardDescription>
                    </div>
                    <Button type="button" onClick={openCreate}>
                        <Plus />
                        {t('inventory.addLocation')}
                    </Button>
                </div>
            </CardHeader>

            <CardContent className="space-y-5">
                {showForm && (
                    <form className="rounded-lg border p-4" onSubmit={submit}>
                        <div className="mb-4 flex items-center justify-between gap-3">
                            <div className="font-medium">
                                {editing ? t('inventory.editLocation') : t('inventory.newLocation')}
                            </div>
                            <Button type="button" size="icon" variant="ghost" onClick={closeForm}>
                                <X />
                            </Button>
                        </div>

                        <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
                            <Field label={t('inventory.locationCode')} value={form.code} onChange={(value) => setForm((s) => ({ ...s, code: value }))} disabled={Boolean(editing)} />
                            <Field label={t('inventory.locationName')} value={form.name} onChange={(value) => setForm((s) => ({ ...s, name: value }))} />
                            <Field label={t('inventory.zone')} value={form.zone} onChange={(value) => setForm((s) => ({ ...s, zone: value }))} />
                            <Field label={t('inventory.rack')} value={form.rack} onChange={(value) => setForm((s) => ({ ...s, rack: value }))} />
                            <Field label={t('inventory.shelf')} value={form.shelf} onChange={(value) => setForm((s) => ({ ...s, shelf: value }))} />
                            <Field label={t('inventory.bin')} value={form.bin} onChange={(value) => setForm((s) => ({ ...s, bin: value }))} />
                        </div>

                        <div className="mt-4 flex flex-wrap justify-end gap-2">
                            <Button type="button" variant="outline" onClick={closeForm}>
                                {t('inventory.cancel')}
                            </Button>
                            <Button type="submit" disabled={createMutation.isPending || updateMutation.isPending}>
                                {editing ? t('inventory.saveChanges') : t('inventory.createLocation')}
                            </Button>
                        </div>
                    </form>
                )}

                {query.isLoading && <div className="text-muted-foreground text-sm">{t('inventory.loading')}</div>}
                {query.isError && <div className="text-destructive text-sm">{query.error instanceof Error ? query.error.message : t('inventory.locationsLoadError')}</div>}

                {query.isSuccess && query.data.length === 0 && (
                    <div className="rounded-lg border border-dashed p-6 text-center text-sm text-muted-foreground">
                        {t('inventory.noLocations')}
                    </div>
                )}

                {query.isSuccess && query.data.length > 0 && (
                    <div className="overflow-x-auto">
                        <table className="w-full text-sm">
                            <thead>
                                <tr className="border-b text-start">
                                    <th className="px-3 py-3 text-start">{t('inventory.locationCode')}</th>
                                    <th className="px-3 py-3 text-start">{t('inventory.locationName')}</th>
                                    <th className="px-3 py-3 text-start">{t('inventory.locationPath')}</th>
                                    <th className="px-3 py-3 text-start">{t('inventory.status')}</th>
                                    <th className="px-3 py-3 text-end">{t('inventory.actions')}</th>
                                </tr>
                            </thead>
                            <tbody>
                                {query.data.map((location) => (
                                    <tr key={location.id} className="border-b last:border-0">
                                        <td className="px-3 py-3 font-medium">{location.code}</td>
                                        <td className="px-3 py-3">{location.name}</td>
                                        <td className="px-3 py-3 text-muted-foreground">
                                            {[location.zone, location.rack, location.shelf, location.bin].filter(Boolean).join(' / ') || '—'}
                                        </td>
                                        <td className="px-3 py-3">
                                            <Badge variant={location.isActive ? 'default' : 'secondary'}>
                                                {location.isActive ? t('inventory.active') : t('inventory.inactive')}
                                            </Badge>
                                        </td>
                                        <td className="px-3 py-3 text-end">
                                            <div className="flex justify-end gap-1">
                                                <Button size="icon" variant="ghost" onClick={() => openEdit(location)}>
                                                    <Pencil />
                                                </Button>
                                                <Button
                                                    size="icon"
                                                    variant="ghost"
                                                    disabled={statusMutation.isPending}
                                                    onClick={async () => {
                                                        try {
                                                            await statusMutation.mutateAsync({ warehouseId, locationId: location.id, isActive: !location.isActive });
                                                            toast.success(t(location.isActive ? 'inventory.locationDeactivated' : 'inventory.locationActivated'));
                                                        } catch (error) {
                                                            toast.error(error instanceof Error ? error.message : t('inventory.locationActionError'));
                                                        }
                                                    }}
                                                >
                                                    {location.isActive ? <PowerOff /> : <Power />}
                                                </Button>
                                            </div>
                                        </td>
                                    </tr>
                                ))}
                            </tbody>
                        </table>
                    </div>
                )}
            </CardContent>
        </Card>
    );
}

function Field({
    label,
    value,
    onChange,
    disabled = false,
}: {
    label: string;
    value: string;
    onChange: (value: string) => void;
    disabled?: boolean;
}) {
    const id = `location-${label.replace(/\W+/g, '-').toLowerCase()}`;
    return (
        <div className="grid gap-2">
            <Label htmlFor={id}>{label}</Label>
            <Input id={id} value={value} onChange={(event) => onChange(event.target.value)} disabled={disabled} />
        </div>
    );
}
