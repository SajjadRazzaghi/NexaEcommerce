import {
    useQuery,
} from '@tanstack/react-query';

import {
    getFulfillmentQueue,
} from '../api/fulfillmentQueueApi';

export const fulfillmentQueueQueryKey =
    (
        skip: number,
        take: number,
    ) => [
        'admin',
        'fulfillment',
        'queue',
        {
            skip,
            take,
        },
    ] as const;

export function useFulfillmentQueue(
    skip = 0,
    take = 50,
) {
    return useQuery({
        queryKey:
            fulfillmentQueueQueryKey(
                skip,
                take,
            ),
        queryFn: () =>
            getFulfillmentQueue(
                skip,
                take,
            ),
        refetchInterval: 5000,
        staleTime: 2000,
    });
}