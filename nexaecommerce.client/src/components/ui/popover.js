import { jsx as _jsx } from "react/jsx-runtime";
import * as React from 'react';
import { Popover as PopoverPrimitive } from 'radix-ui';
import { cn } from '@/lib/utils';
const Popover = PopoverPrimitive.Root;
const PopoverTrigger = PopoverPrimitive.Trigger;
const PopoverAnchor = PopoverPrimitive.Anchor;
function PopoverContent({ className, align = 'center', sideOffset = 4, ...props }) {
    return (_jsx(PopoverPrimitive.Portal, { children: _jsx(PopoverPrimitive.Content, { "data-slot": "popover-content", align: align, sideOffset: sideOffset, className: cn('bg-popover text-popover-foreground z-50 w-72 origin-(--radix-popover-content-transform-origin) rounded-xl border p-1 shadow-lg outline-none', className), ...props }) }));
}
export { Popover, PopoverTrigger, PopoverContent, PopoverAnchor };
