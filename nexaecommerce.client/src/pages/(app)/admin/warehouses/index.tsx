import { useMemo, useState } from 'react';
import { Link, useNavigate } from 'react-router';
import { useTranslation } from 'react-i18next';
import {
  CheckCircle2,
  MoreHorizontal,
  Pencil,
  Plus,
  Power,
  PowerOff,
  Search,
  Warehouse as WarehouseIcon,
} from 'lucide-react';
import { toast } from 'sonner';

import {
  PageHeader,
  EmptyState,
  ErrorState,
  LoadingSkeleton,
} from '@/components/data-states';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Card, CardContent } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';
import { usePermission } from '@/hooks/use-permission';
import {
  INVENTORY_PERM,
  type Warehouse,
} from '@/lib/api/inventory';
import {
  useSetDefaultWarehouse,
  useSetWarehouseStatus,
  useWarehouses,
} from '@/modules/inventory/hooks/useWarehouses';
import { useDocumentTitle } from '@/hooks/use-document-title';

type Filter = 'all' | 'active' | 'inactive';

export default function WarehousesPage() {
  const { t } = useTranslation();
  const navigate = useNavigate();

  useDocumentTitle(t('warehouses.title'));

  const canManage = usePermission(INVENTORY_PERM.manage);
  const query = useWarehouses(true);
  const setStatus = useSetWarehouseStatus();
  const setDefault = useSetDefaultWarehouse();

  const [search, setSearch] = useState('');
  const [filter, setFilter] = useState<Filter>('all');

  const filtered = useMemo(() => {
    const term = search.trim().toLocaleLowerCase();

    return (
      query.data?.filter((warehouse) => {
        const matchesSearch =
          !term ||
          warehouse.name.toLocaleLowerCase().includes(term) ||
          warehouse.code.toLocaleLowerCase().includes(term) ||
          warehouse.city?.toLocaleLowerCase().includes(term);

        const matchesFilter =
          filter === 'all' ||
          (filter === 'active' && warehouse.isActive) ||
          (filter === 'inactive' && !warehouse.isActive);

        return matchesSearch && matchesFilter;
      }) ?? []
    );
  }, [query.data, search, filter]);

  const run = async (
    promise: Promise<unknown>,
    successMessage: string,
  ) => {
    try {
      await promise;
      toast.success(successMessage);
    } catch (error) {
      toast.error(
        error instanceof Error
          ? error.message
          : t('warehouses.actionError'),
      );
    }
  };

  return (
    <div className="space-y-6">
      <PageHeader
        title={t('warehouses.title')}
        description={t('warehouses.description')}
        actions={
          canManage ? (
            <Button asChild>
              <Link to="/admin/warehouses/new">
                <Plus />
                {t('warehouses.new')}
              </Link>
            </Button>
          ) : null
        }
      />

      <Card>
        <CardContent className="space-y-4 pt-6">
          <div className="flex flex-col gap-3 md:flex-row md:items-center md:justify-between">
            <div className="relative w-full md:max-w-md">
              <Search className="text-muted-foreground absolute start-3 top-1/2 size-4 -translate-y-1/2" />
              <Input
                className="ps-9"
                value={search}
                onChange={(event) => setSearch(event.target.value)}
                placeholder={t('warehouses.searchPlaceholder')}
              />
            </div>

            <div className="flex flex-wrap gap-2">
              {(['all', 'active', 'inactive'] as const).map((item) => (
                <Button
                  key={item}
                  size="sm"
                  variant={filter === item ? 'default' : 'outline'}
                  onClick={() => setFilter(item)}
                >
                  {t(
                    item === 'all'
                      ? 'warehouses.all'
                      : item === 'active'
                        ? 'warehouses.onlyActive'
                        : 'warehouses.onlyInactive',
                  )}
                </Button>
              ))}
            </div>
          </div>

          {query.isLoading && (
            <LoadingSkeleton variant="table" rows={6} cols={5} />
          )}

          {query.isError && (
            <ErrorState
              error={query.error}
              onRetry={() => query.refetch()}
              message={t('warehouses.loadError')}
            />
          )}

          {query.isSuccess && query.data.length === 0 && (
            <EmptyState
              icon={WarehouseIcon}
              title={t('warehouses.emptyTitle')}
              description={t('warehouses.emptyDescription')}
              action={
                canManage ? (
                  <Button asChild>
                    <Link to="/admin/warehouses/new">
                      <Plus />
                      {t('warehouses.new')}
                    </Link>
                  </Button>
                ) : undefined
              }
            />
          )}

          {query.isSuccess &&
            query.data.length > 0 &&
            filtered.length === 0 && (
              <EmptyState
                icon={Search}
                title={t('warehouses.noResults')}
                description={t('warehouses.noResultsDescription')}
              />
            )}

          {query.isSuccess && filtered.length > 0 && (
            <div className="overflow-x-auto">
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead className="whitespace-nowrap">
                      {t('warehouses.nameAndCode')}
                    </TableHead>
                    <TableHead>{t('warehouses.location')}</TableHead>
                    <TableHead>{t('warehouses.contact')}</TableHead>
                    <TableHead className="whitespace-nowrap">
                      {t('warehouses.status')}
                    </TableHead>
                    {canManage && (
                      <TableHead className="text-end">
                        {t('warehouses.actions')}
                      </TableHead>
                    )}
                  </TableRow>
                </TableHeader>

                <TableBody>
                  {filtered.map((warehouse) => (
                    <WarehouseRow
                      key={warehouse.id}
                      warehouse={warehouse}
                      canManage={canManage}
                      onEdit={() =>
                        navigate(`/admin/warehouses/${warehouse.id}/edit`)
                      }
                      onToggleStatus={() =>
                        run(
                          setStatus.mutateAsync({
                            id: warehouse.id,
                            isActive: !warehouse.isActive,
                          }),
                          t(
                            warehouse.isActive
                              ? 'warehouses.deactivated'
                              : 'warehouses.activated',
                          ),
                        )
                      }
                      onSetDefault={() =>
                        run(
                          setDefault.mutateAsync(warehouse.id),
                          t('warehouses.defaultUpdated'),
                        )
                      }
                    />
                  ))}
                </TableBody>
              </Table>
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  );
}

function WarehouseRow({
  warehouse,
  canManage,
  onEdit,
  onToggleStatus,
  onSetDefault,
}: {
  warehouse: Warehouse;
  canManage: boolean;
  onEdit: () => void;
  onToggleStatus: () => void;
  onSetDefault: () => void;
}) {
  const { t } = useTranslation();

  return (
    <TableRow>
      <TableCell>
        <div className="min-w-0">
          <div className="flex flex-wrap items-center gap-2">
            <span className="font-medium">{warehouse.name}</span>
            {warehouse.isDefault && (
              <Badge variant="outline">
                <CheckCircle2 />
                {t('warehouses.defaultBadge')}
              </Badge>
            )}
          </div>
          <div className="text-muted-foreground mt-1 text-xs">
            {warehouse.code}
          </div>
        </div>
      </TableCell>

      <TableCell>
        <div className="grid gap-1 text-sm">
          <span>{warehouse.city ?? '—'}</span>
          {warehouse.addressLine && (
            <span className="text-muted-foreground max-w-72 truncate">
              {warehouse.addressLine}
            </span>
          )}
        </div>
      </TableCell>

      <TableCell>
        <div className="grid gap-1 text-sm">
          <span>{warehouse.phone ?? '—'}</span>
          <span className="text-muted-foreground">
            {warehouse.postalCode ?? '—'}
          </span>
        </div>
      </TableCell>

      <TableCell>
        <Badge variant={warehouse.isActive ? 'default' : 'secondary'}>
          {warehouse.isActive
            ? t('warehouses.active')
            : t('warehouses.inactive')}
        </Badge>
      </TableCell>

      {canManage && (
        <TableCell className="text-end">
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button variant="ghost" size="icon">
                <MoreHorizontal />
              </Button>
            </DropdownMenuTrigger>

            <DropdownMenuContent align="end">
              <DropdownMenuItem onClick={onEdit}>
                <Pencil />
                {t('actions.edit')}
              </DropdownMenuItem>

              {!warehouse.isDefault && (
                <DropdownMenuItem
                  disabled={!warehouse.isActive}
                  onClick={onSetDefault}
                >
                  <CheckCircle2 />
                  {t('warehouses.setDefault')}
                </DropdownMenuItem>
              )}

              <DropdownMenuSeparator />

              {warehouse.isActive ? (
                <DropdownMenuItem
                  disabled={warehouse.isDefault}
                  onClick={onToggleStatus}
                >
                  <PowerOff />
                  {t('warehouses.deactivate')}
                </DropdownMenuItem>
              ) : (
                <DropdownMenuItem onClick={onToggleStatus}>
                  <Power />
                  {t('warehouses.activate')}
                </DropdownMenuItem>
              )}
            </DropdownMenuContent>
          </DropdownMenu>
        </TableCell>
      )}
    </TableRow>
  );
}
