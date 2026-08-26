import { jsx as _jsx } from "react/jsx-runtime";
import i18n from '@/i18n.config';
import { Checkbox } from '@/components/ui/checkbox';
/** Leading checkbox column: select-all-on-page in the header, per-row in the body. */
export function selectColumn() {
    return {
        id: '__select',
        enableSorting: false,
        enableHiding: false,
        meta: { label: '' },
        header: ({ table }) => (_jsx(Checkbox, { checked: table.getIsAllPageRowsSelected()
                ? true
                : table.getIsSomePageRowsSelected()
                    ? 'indeterminate'
                    : false, onCheckedChange: (value) => table.toggleAllPageRowsSelected(!!value), onClick: (e) => e.stopPropagation(), "aria-label": i18n.t('grid.selectAll') })),
        cell: ({ row }) => (_jsx(Checkbox, { checked: row.getIsSelected(), onCheckedChange: (value) => row.toggleSelected(!!value), onClick: (e) => e.stopPropagation(), "aria-label": i18n.t('grid.selectRow') })),
    };
}
