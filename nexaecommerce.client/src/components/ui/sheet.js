import { jsx as _jsx, jsxs as _jsxs } from "react/jsx-runtime";
import * as React from 'react';
import { Dialog as SheetPrimitive } from 'radix-ui';
import { XIcon } from 'lucide-react';
import { cn } from '@/lib/utils';
const Sheet = SheetPrimitive.Root;
const SheetTrigger = SheetPrimitive.Trigger;
const SheetClose = SheetPrimitive.Close;
function SheetOverlay({ className, ...props }) {
    return (_jsx(SheetPrimitive.Overlay, { "data-slot": "sheet-overlay", className: cn('fixed inset-0 z-50 bg-black/50', className), ...props }));
}
// A side panel built on the Dialog primitive: used for the mobile nav drawer. `side` controls which
// edge it docks to; logical insets (start/end) keep it correct under RTL.
function SheetContent({ className, children, side = 'left', ...props }) {
    return (_jsxs(SheetPrimitive.Portal, { children: [_jsx(SheetOverlay, {}), _jsxs(SheetPrimitive.Content, { "data-slot": "sheet-content", className: cn('bg-background fixed z-50 flex h-svh w-3/4 max-w-xs flex-col gap-0 border-e shadow-lg', side === 'left' ? 'inset-y-0 start-0' : 'inset-y-0 end-0 border-s border-e-0', className), ...props, children: [children, _jsxs(SheetPrimitive.Close, { className: "ring-offset-background focus-visible:ring-ring absolute end-4 top-4 rounded-xs opacity-70 transition-opacity hover:opacity-100 focus-visible:ring-2 focus-visible:outline-none", children: [_jsx(XIcon, { className: "size-4" }), _jsx("span", { className: "sr-only", children: "Close" })] })] })] }));
}
function SheetHeader({ className, ...props }) {
    return _jsx("div", { "data-slot": "sheet-header", className: cn('flex flex-col gap-1.5 p-4', className), ...props });
}
function SheetTitle({ className, ...props }) {
    return _jsx(SheetPrimitive.Title, { "data-slot": "sheet-title", className: cn('font-semibold', className), ...props });
}
function SheetDescription({ className, ...props }) {
    return (_jsx(SheetPrimitive.Description, { "data-slot": "sheet-description", className: cn('text-muted-foreground text-sm', className), ...props }));
}
export { Sheet, SheetTrigger, SheetClose, SheetContent, SheetHeader, SheetTitle, SheetDescription };
