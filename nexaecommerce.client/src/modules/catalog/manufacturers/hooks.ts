import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useAuth } from '@/hooks/use-auth';
import { manufacturersApi, type ManufacturerFilter } from '@/modules/catalog/api/manufacturers';

export const manufacturerKeys = {
  all: ['manufacturers'] as const,
  lists: () => [...manufacturerKeys.all, 'list'] as const,
  list: (filter: ManufacturerFilter) => [...manufacturerKeys.lists(), filter] as const,
  details: () => [...manufacturerKeys.all, 'detail'] as const,
  detail: (id: string) => [...manufacturerKeys.details(), id] as const,
  lookup: () => [...manufacturerKeys.all, 'lookup'] as const,
};

export function useManufacturers(filter: ManufacturerFilter) {
  const { user, isLoading } = useAuth();

  return useQuery({
    queryKey: manufacturerKeys.list(filter),
    queryFn: ({ signal }) => manufacturersApi.list(filter, signal),
    enabled: !isLoading && !!user,
  });
}

export function useManufacturer(id?: string) {
  const { user, isLoading } = useAuth();

  return useQuery({
    queryKey: manufacturerKeys.detail(id ?? ''),
    queryFn: () => manufacturersApi.get(id!),
    enabled: !isLoading && !!user && !!id,
  });
}

export function useManufacturerLookup() {
  return useQuery({
    queryKey: manufacturerKeys.lookup(),
    queryFn: manufacturersApi.lookup,
    staleTime: 5 * 60 * 1000,
  });
}

export function useCreateManufacturer() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: manufacturersApi.create,
    onSuccess: () => qc.invalidateQueries({ queryKey: manufacturerKeys.all }),
  });
}

export function useUpdateManufacturer() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, body }: { id: string; body: Parameters<typeof manufacturersApi.update>[1] }) =>
      manufacturersApi.update(id, body),
    onSuccess: (_, variables) => {
      qc.invalidateQueries({ queryKey: manufacturerKeys.all });
      qc.invalidateQueries({ queryKey: manufacturerKeys.detail(variables.id) });
    },
  });
}

export function useManufacturerAction(
  action: keyof Pick<
    typeof manufacturersApi,
    'remove' | 'restore' | 'activate' | 'deactivate' | 'publish' | 'unpublish' | 'feature' | 'unfeature'
  >,
) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => manufacturersApi[action](id) as Promise<void>,
    onSuccess: (_, id) => {
      qc.invalidateQueries({ queryKey: manufacturerKeys.all });
      qc.invalidateQueries({ queryKey: manufacturerKeys.detail(id) });
    },
  });
}
