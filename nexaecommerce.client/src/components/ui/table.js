import { jsx as _jsx } from "react/jsx-runtime";
import * as React from 'react';
import { cn } from '@/lib/utils';
function Table({ className, ...props }) {
    return (_jsx("div", { "data-slot": "table-container", className: "relative w-full overflow-x-auto", children: _jsx("table", { "data-slot": "table", className: cn('w-full caption-bottom text-sm', className), ...props }) }));
}
function TableHeader({ className, ...props }) {
    return _jsx("thead", { "data-slot": "table-header", className: cn('[&_tr]:border-b', className), ...props });
}
function TableBody({ className, ...props }) {
    return _jsx("tbody", { "data-slot": "table-body", className: cn('[&_tr:last-child]:border-0', className), ...props });
}
function TableRow({ className, ...props }) {
    return (_jsx("tr", { "data-slot": "table-row", className: cn('hover:bg-muted/50 data-[state=selected]:bg-muted border-b transition-colors', className), ...props }));
}
function TableHead({ className, ...props }) {
    return (_jsx("th", { "data-slot": "table-head", className: cn('text-muted-foreground h-10 px-2 text-start align-middle font-medium whitespace-nowrap [&:has([role=checkbox])]:pe-0', className), ...props }));
}
function TableCell({ className, ...props }) {
    return (_jsx("td", { "data-slot": "table-cell", className: cn('p-2 align-middle whitespace-nowrap [&:has([role=checkbox])]:pe-0', className), ...props }));
}
export { Table, TableHeader, TableBody, TableRow, TableHead, TableCell };
