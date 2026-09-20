/* eslint-disable react-hooks/set-state-in-effect */

import {
  useEffect,
  useState,
  type FormEvent,
} from 'react';
import { useTranslation } from 'react-i18next';

import {
  type UpdateWarehouseRequest,
  type Warehouse,
} from '@/lib/api/inventory';
import { Button } from '@/components/ui/button';
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from '@/components/ui/card';
import { Checkbox } from '@/components/ui/checkbox';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';

type WarehouseFormMode = 'create' | 'edit';

export interface WarehouseFormSubmitBody {
  code: string;
  name: string;
  addressLine: string | null;
  city: string | null;
  postalCode: string | null;
  phone: string | null;
  isDefault?: boolean;
}

interface WarehouseFormProps {
  mode: WarehouseFormMode;
  warehouse?: Warehouse | null;
  pending?: boolean;
  onCancel: () => void;
  onSubmit: (
    body: WarehouseFormSubmitBody | UpdateWarehouseRequest,
  ) => Promise<void>;
}

export default function WarehouseForm({
  mode,
  warehouse,
  pending = false,
  onCancel,
  onSubmit,
}: WarehouseFormProps) {
  const { t } = useTranslation();

  const [code, setCode] = useState(warehouse?.code ?? '');
  const [name, setName] = useState(warehouse?.name ?? '');
  const [addressLine, setAddressLine] = useState(warehouse?.addressLine ?? '');
  const [city, setCity] = useState(warehouse?.city ?? '');
  const [postalCode, setPostalCode] = useState(warehouse?.postalCode ?? '');
  const [phone, setPhone] = useState(warehouse?.phone ?? '');
  const [isDefault, setIsDefault] = useState(warehouse?.isDefault ?? false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    setCode(warehouse?.code ?? '');
    setName(warehouse?.name ?? '');
    setAddressLine(warehouse?.addressLine ?? '');
    setCity(warehouse?.city ?? '');
    setPostalCode(warehouse?.postalCode ?? '');
    setPhone(warehouse?.phone ?? '');
    setIsDefault(warehouse?.isDefault ?? false);
    setError(null);
  }, [warehouse]);

  const submit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setError(null);

    const normalizedCode = code.trim().toUpperCase();
    const normalizedName = name.trim();

    if (!normalizedCode) {
      setError(t('warehouses.validationCode'));
      return;
    }

    if (!normalizedName) {
      setError(t('warehouses.validationName'));
      return;
    }

    try {
      await onSubmit({
        code: normalizedCode,
        name: normalizedName,
        addressLine: addressLine.trim() || null,
        city: city.trim() || null,
        postalCode: postalCode.trim() || null,
        phone: phone.trim() || null,
        ...(mode === 'create' ? { isDefault } : {}),
      });
    } catch (submitError) {
      setError(
        submitError instanceof Error
          ? submitError.message
          : t('warehouses.saveError'),
      );
    }
  };

  return (
    <Card>
      <CardHeader>
        <CardTitle>{t('warehouses.basicInfo')}</CardTitle>
        <CardDescription>
          {t(
            mode === 'create'
              ? 'warehouses.createDescription'
              : 'warehouses.editDescription',
          )}
        </CardDescription>
      </CardHeader>

      <CardContent>
        <form className="grid gap-6" onSubmit={submit}>
          {error && (
            <div
              role="alert"
              className="rounded-lg border border-destructive/30 bg-destructive/10 px-3 py-2 text-sm text-destructive"
            >
              {error}
            </div>
          )}

          <div className="grid gap-4 md:grid-cols-2">
            <Field
              id="warehouse-code"
              label={t('warehouses.code')}
              value={code}
              onChange={setCode}
              placeholder={t('warehouses.codePlaceholder')}
              required
              disabled={pending || mode === 'edit'}
            />

            <Field
              id="warehouse-name"
              label={t('warehouses.name')}
              value={name}
              onChange={setName}
              placeholder={t('warehouses.namePlaceholder')}
              required
              disabled={pending}
            />

            <Field
              id="warehouse-address"
              label={t('warehouses.address')}
              value={addressLine}
              onChange={setAddressLine}
              placeholder={t('warehouses.addressPlaceholder')}
              disabled={pending}
            />

            <Field
              id="warehouse-city"
              label={t('warehouses.city')}
              value={city}
              onChange={setCity}
              placeholder={t('warehouses.cityPlaceholder')}
              disabled={pending}
            />

            <Field
              id="warehouse-postal-code"
              label={t('warehouses.postalCode')}
              value={postalCode}
              onChange={setPostalCode}
              disabled={pending}
            />

            <Field
              id="warehouse-phone"
              label={t('warehouses.phone')}
              value={phone}
              onChange={setPhone}
              disabled={pending}
            />
          </div>

          {mode === 'create' && (
            <label className="flex cursor-pointer items-start gap-3 rounded-lg border p-4">
              <Checkbox
                checked={isDefault}
                onCheckedChange={(value) => setIsDefault(value === true)}
                disabled={pending}
                className="mt-0.5"
              />
              <span className="grid gap-1">
                <span className="font-medium">{t('warehouses.default')}</span>
                <span className="text-muted-foreground text-sm">
                  {t('warehouses.defaultHint')}
                </span>
              </span>
            </label>
          )}

          {mode === 'edit' && warehouse && (
            <div className="rounded-lg border px-4 py-3 text-sm">
              <span className="font-medium">{t('warehouses.status')}:</span>{' '}
              {warehouse.isActive
                ? t('warehouses.active')
                : t('warehouses.inactive')}
              {warehouse.isDefault && (
                <>
                  {' · '}
                  {t('warehouses.defaultBadge')}
                </>
              )}
            </div>
          )}

          <div className="flex flex-wrap justify-end gap-2">
            <Button
              type="button"
              variant="outline"
              onClick={onCancel}
              disabled={pending}
            >
              {t('warehouses.cancel')}
            </Button>
            <Button type="submit" disabled={pending}>
              {pending ? t('warehouses.loading') : t('warehouses.save')}
            </Button>
          </div>
        </form>
      </CardContent>
    </Card>
  );
}

function Field({
  id,
  label,
  value,
  onChange,
  placeholder,
  required = false,
  disabled = false,
}: {
  id: string;
  label: string;
  value: string;
  onChange: (value: string) => void;
  placeholder?: string;
  required?: boolean;
  disabled?: boolean;
}) {
  return (
    <div className="grid gap-2">
      <Label htmlFor={id}>
        {label}
        {required ? ' *' : ''}
      </Label>
      <Input
        id={id}
        value={value}
        onChange={(event) => onChange(event.target.value)}
        placeholder={placeholder}
        required={required}
        disabled={disabled}
      />
    </div>
  );
}
