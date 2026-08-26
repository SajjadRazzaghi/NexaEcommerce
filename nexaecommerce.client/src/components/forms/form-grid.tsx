// src/components/forms/form-grid.tsx
import type { ReactNode } from 'react';

import { cn } from '@/lib/utils';

type FormGridColumns = 1 | 2 | 3 | 4;

export function FormGrid({
    columns = 1,
    className,
    children,
}: {
    columns?: FormGridColumns;
    className?: string;
    children: ReactNode;
}) {
    const gridColumns = {
        1: 'grid-cols-1',
        2: 'grid-cols-1 sm:grid-cols-2',
        3: 'grid-cols-1 sm:grid-cols-2 lg:grid-cols-3',
        4: 'grid-cols-1 sm:grid-cols-2 lg:grid-cols-4',
    }[columns];

    return (
        <div className={cn('grid gap-4', gridColumns, className)}>
            {children}
        </div>
    );
}