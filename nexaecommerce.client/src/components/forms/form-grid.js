import { jsx as _jsx } from "react/jsx-runtime";
import { cn } from '@/lib/utils';
/** Responsive field layout: stacks on mobile, optionally two columns from `sm` up. */
export function FormGrid({ columns = 1, className, children, }) {
    return (_jsx("div", { className: cn('grid gap-4', columns === 2 && 'sm:grid-cols-2', className), children: children }));
}
