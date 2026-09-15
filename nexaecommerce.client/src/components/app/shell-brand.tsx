import {
    useQuery,
} from '@tanstack/react-query';

import {
    Brand,
} from '@/components/brand';

import {
    appearanceApi,
} from '@/lib/api/appearance';

export function ShellBrand({
    className,
    markOnly,
}: {
    className?: string;
    markOnly?: boolean;
}) {
    const {
        data,
    } = useQuery({
        queryKey: [
            'appearance',
        ],

        queryFn:
            appearanceApi.get,

        staleTime:
            5 * 60_000,

        retry: 1,
    });

    const name =
        data?.storeName ??
        undefined;

    const logoUrl =
        data?.logoUrl ??
        null;

    return (
        <Brand
            className={
                className
            }
            markOnly={
                markOnly
            }
            name={
                name
            }
            logoUrl={
                logoUrl
            }
        />
    );
}