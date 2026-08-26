import { jsx as _jsx, jsxs as _jsxs } from "react/jsx-runtime";
import * as React from 'react';
import { Dialog as DialogPrimitive } from 'radix-ui';
import { XIcon } from 'lucide-react';
import { cn } from '@/lib/utils';
const Dialog = DialogPrimitive.Root;
const DialogTrigger = DialogPrimitive.Trigger;
const DialogClose = DialogPrimitive.Close;
function DialogOverlay({ className, ...props }) {
    return (_jsx(DialogPrimitive.Overlay, { "data-slot": "dialog-overlay", className: cn('fixed inset-0 z-50 bg-black/50 data-[state=open]:animate-in data-[state=closed]:animate-out data-[state=closed]:fade-out-0 data-[state=open]:fade-in-0', className), ...props }));
}
function DialogContent({ className, children, ...props }) {
    return (_jsxs(DialogPrimitive.Portal, { children: [_jsx(DialogOverlay, {}), _jsxs(DialogPrimitive.Content, { "data-slot": "dialog-content", className: cn('bg-card fixed top-1/2 left-1/2 z-50 grid w-full max-w-[calc(100%-2rem)] -translate-x-1/2 -translate-y-1/2 gap-4 rounded-xl border p-6 shadow-lg duration-200 data-[state=open]:animate-in data-[state=closed]:animate-out data-[state=closed]:fade-out-0 data-[state=open]:fade-in-0 data-[state=closed]:zoom-out-95 data-[state=open]:zoom-in-95 sm:max-w-lg', className), ...props, children: [children, _jsxs(DialogPrimitive.Close, { className: "ring-offset-background focus-visible:ring-ring data-[state=open]:bg-accent absolute end-4 top-4 rounded-xs opacity-70 transition-opacity hover:opacity-100 focus-visible:ring-2 focus-visible:ring-offset-2 focus-visible:outline-none disabled:pointer-events-none", children: [_jsx(XIcon, { className: "size-4" }), _jsx("span", { className: "sr-only", children: "Close" })] })] })] }));
}
function DialogHeader({ className, ...props }) {
    return _jsx("div", { "data-slot": "dialog-header", className: cn('grid gap-1.5 text-center sm:text-left', className), ...props });
}
function DialogFooter({ className, ...props }) {
    return (_jsx("div", { "data-slot": "dialog-footer", className: cn('flex flex-col-reverse gap-2 sm:flex-row sm:justify-end', className), ...props }));
}
function DialogTitle({ className, ...props }) {
    return _jsx(DialogPrimitive.Title, { "data-slot": "dialog-title", className: cn('text-lg font-semibold', className), ...props });
}
function DialogDescription({ className, ...props }) {
    return (_jsx(DialogPrimitive.Description, { "data-slot": "dialog-description", className: cn('text-muted-foreground text-sm', className), ...props }));
}
export { Dialog, DialogTrigger, DialogClose, DialogContent, DialogHeader, DialogFooter, DialogTitle, DialogDescription, };
