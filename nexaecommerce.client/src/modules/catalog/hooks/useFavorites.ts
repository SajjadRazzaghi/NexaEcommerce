import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';

import { useAuth } from '@/hooks/use-auth';
import { favoritesApi } from '../api/favorites';

export const favoritesQueryRoot = ['favorites', 'mine'] as const;

export function useFavorites() {
    const queryClient = useQueryClient();
    const { isAuthenticated } = useAuth();
    const queryKey = [...favoritesQueryRoot, isAuthenticated] as const;

    const query = useQuery({
        queryKey,
        queryFn: favoritesApi.mine,
        staleTime: 0,
        retry: 1,
        refetchOnWindowFocus: true,
    });

    const toggleMutation = useMutation({
        mutationFn: async ({
            productId,
            isFavorite,
        }: {
            productId: string;
            isFavorite: boolean;
        }): Promise<void> => {
            if (isFavorite) {
                await favoritesApi.remove(productId);
            } else {
                await favoritesApi.add(productId);
            }
        },

        onSuccess: async () => {
            await queryClient.invalidateQueries({
                queryKey: favoritesQueryRoot,
            });
        },
    });

    const toggleFavorite = (productId: string, isFavorite: boolean) =>
        toggleMutation.mutateAsync({ productId, isFavorite });

    return {
        ...query,
        items: query.data?.items ?? [],
        ids: new Set(query.data?.ids ?? []),
        total: query.data?.total ?? 0,
        toggleFavorite,
        isToggling: toggleMutation.isPending,
    };
}
