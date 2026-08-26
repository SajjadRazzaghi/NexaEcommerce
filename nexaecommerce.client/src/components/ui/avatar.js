import { jsx as _jsx } from "react/jsx-runtime";
import * as React from 'react';
import { Avatar as AvatarPrimitive } from 'radix-ui';
import { cn } from '@/lib/utils';
function Avatar({ className, ...props }) {
    return (_jsx(AvatarPrimitive.Root, { "data-slot": "avatar", className: cn('relative flex size-9 shrink-0 overflow-hidden rounded-full', className), ...props }));
}
function AvatarImage({ className, ...props }) {
    return (_jsx(AvatarPrimitive.Image, { "data-slot": "avatar-image", className: cn('aspect-square size-full', className), ...props }));
}
function AvatarFallback({ className, ...props }) {
    return (_jsx(AvatarPrimitive.Fallback, { "data-slot": "avatar-fallback", className: cn('bg-muted text-muted-foreground flex size-full items-center justify-center rounded-full text-sm font-medium', className), ...props }));
}
export { Avatar, AvatarImage, AvatarFallback };
