import { jsx as _jsx, jsxs as _jsxs } from "react/jsx-runtime";
import { Hexagon } from 'lucide-react';
import { cn } from '@/lib/utils';
/**
 * Brand lockup: geometric mark + wordmark. `tone="onDark"` is for placement on the always-dark
 * brand panel, where the theme's primary token would otherwise invert the chip.
 */
export function Brand({ className, markOnly = false, tone = 'default', name, logoUrl, }) {
    const chip = tone === 'onDark' ? 'bg-white/10 text-white' : 'bg-primary text-primary-foreground';
    return (_jsxs("span", { className: cn('inline-flex items-center gap-2 font-semibold tracking-tight', className), children: [logoUrl ? (_jsx("img", { src: logoUrl, alt: "", className: "size-7 shrink-0 rounded-lg object-cover" })) : (_jsx("span", { className: cn('grid size-7 shrink-0 place-items-center rounded-lg', chip), children: _jsx(Hexagon, { className: "size-4" }) })), !markOnly && _jsx("span", { className: "truncate", children: name ?? 'NexaECommerce' })] }));
}
