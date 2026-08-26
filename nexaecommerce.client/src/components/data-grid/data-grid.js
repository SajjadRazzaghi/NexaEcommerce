import { jsx as _jsx, jsxs as _jsxs, Fragment as _Fragment } from "react/jsx-runtime";
import { useEffect, useRef, useState } from 'react';
import { flexRender, getCoreRowModel, useReactTable, } from '@tanstack/react-table';
import { useTranslation } from 'react-i18next';
import { ArrowDown, ArrowUp, ChevronLeft, ChevronRight, ChevronsUpDown, LayoutGrid, Rows3, Search, SlidersHorizontal, Star, X, } from 'lucide-react';
import { cn } from '@/lib/utils';
import { EmptyState, ErrorState, LoadingSkeleton } from '@/components/data-states';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { DropdownMenu, DropdownMenuCheckboxItem, DropdownMenuContent, DropdownMenuItem, DropdownMenuLabel, DropdownMenuSeparator, DropdownMenuTrigger, } from '@/components/ui/dropdown-menu';
const PAGE_SIZES = [10, 20, 50, 100];
export function DataGrid({ grid, columns, getRowId, searchPlaceholder, bulkActions, toolbar, onRowClick, empty, viewKey, enableColumnHiding = true, initialColumnVisibility, exportable = false, renderCard, defaultView = 'table', }) {
    const { t } = useTranslation();
    const [rowSelection, setRowSelection] = useState({});
    // Table vs card view (only when renderCard is provided); the choice persists per viewKey, falling back to
    // defaultView on the first visit (before any saved choice).
    const [view, setView] = useState(() => {
        if (!renderCard)
            return 'table';
        const saved = viewKey ? localStorage.getItem(`nexaecommerce:grid-view:${viewKey}`) : null;
        return saved === 'cards' || saved === 'table' ? saved : defaultView;
    });
    const setViewMode = (next) => {
        setView(next);
        if (viewKey)
            localStorage.setItem(`nexaecommerce:grid-view:${viewKey}`, next);
    };
    // Each view remembers its own visible columns. The table starts from the page's defaults (e.g. a
    // verbose column shipped hidden) and seeds from the pinned default view on a bare URL; the card
    // starts with everything shown so its details are rich. The Columns menu and the card both read the
    // active map, so toggling a column updates whichever view you're looking at — and switching views
    // restores that view's own selection.
    const [tableColumnVisibility, setTableColumnVisibility] = useState(() => {
        const base = initialColumnVisibility ?? {};
        if (viewKey && isBareUrl()) {
            const def = readDefaultView(viewKey);
            if (def?.state.columnVisibility)
                return { ...base, ...def.state.columnVisibility };
        }
        return base;
    });
    const [cardColumnVisibility, setCardColumnVisibility] = useState({});
    const isCards = view === 'cards' && !!renderCard;
    const columnVisibility = isCards ? cardColumnVisibility : tableColumnVisibility;
    const setColumnVisibility = isCards ? setCardColumnVisibility : setTableColumnVisibility;
    // Apply the pinned default view's query state once on a bare URL. Runs on mount and on remount
    // (navigating away and back), giving "my view persists across navigation"; an explicit shared/
    // filtered link carries params and is left untouched. grid.reset writes the URL (not React state),
    // so this doesn't fight the no-setState-in-effect rule; the matching column visibility is seeded
    // in the table useState initializer above.
    const defaultApplied = useRef(false);
    useEffect(() => {
        if (defaultApplied.current || !viewKey || !isBareUrl())
            return;
        defaultApplied.current = true;
        const def = readDefaultView(viewKey);
        if (def?.state)
            grid.reset(def.state);
        // grid.reset is recreated each render; the ref guard makes this a true once-on-mount effect.
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [viewKey]);
    // TanStack Table's hook isn't annotated for the React Compiler lint; its internal state is stable.
    /* eslint-disable react-hooks/incompatible-library */
    const table = useReactTable({
        data: grid.items,
        columns,
        state: { sorting: grid.sorting, rowSelection, columnVisibility },
        manualSorting: true,
        manualPagination: true,
        manualFiltering: true,
        // Server-side sort reads clearer as a 2-state toggle (asc ↔ desc) — no confusing "unsorted"
        // step that silently falls back to the backend default.
        enableSortingRemoval: false,
        enableRowSelection: true,
        getRowId,
        rowCount: grid.pageInfo?.totalItems ?? 0,
        onSortingChange: grid.setSorting,
        onRowSelectionChange: setRowSelection,
        onColumnVisibilityChange: setColumnVisibility,
        getCoreRowModel: getCoreRowModel(),
    });
    /* eslint-enable react-hooks/incompatible-library */
    const selectedIds = Object.keys(rowSelection);
    const clearSelection = () => setRowSelection({});
    const info = grid.pageInfo;
    const dataColumns = table.getAllLeafColumns().filter((c) => c.id !== '__select' && c.getCanHide());
    // Column ids visible in the active view — handed to renderCard so a card can mirror the Columns menu.
    const visibleColumnIds = new Set(table.getVisibleLeafColumns().map((c) => c.id));
    // A responsive card list rendered via renderCard; reused for the card view (all sizes) and as the
    // mobile fallback for the table view (a table can't render on a phone, and a designed card beats the
    // generic label/value rows).
    const cardList = (className) => renderCard ? (_jsx("ul", { className: cn('grid gap-3', className), children: grid.items.map((item) => (_jsx("li", { onClick: onRowClick ? () => onRowClick(item) : undefined, className: cn(onRowClick && 'cursor-pointer'), children: renderCard(item, visibleColumnIds) }, getRowId(item)))) })) : null;
    // Optional export menu — built in TS so the toolbar JSX needs no build-time conditional. The
    // `if (exportable)` keeps the prop referenced even when export is stripped from the Basic edition.
    const slots = {};
    if (exportable) {
        // The export menu is only built when the Export feature is included in this edition.
    }
    return (_jsxs("div", { className: "space-y-3", children: [_jsxs("div", { className: "flex flex-wrap items-center gap-2", children: [_jsx(SearchBox, { value: grid.search, onChange: grid.setSearch, placeholder: searchPlaceholder ?? `${t('common.search')}…` }), toolbar, _jsxs("div", { className: "ms-auto flex items-center gap-2", children: [grid.isFetching && !grid.isLoading && (_jsx("span", { className: "text-muted-foreground text-xs", children: t('common.loading') })), renderCard && (_jsxs("div", { className: "flex items-center rounded-md border p-0.5", children: [_jsx(Button, { variant: view === 'table' ? 'secondary' : 'ghost', size: "icon", className: "size-7", onClick: () => setViewMode('table'), "aria-label": t('grid.tableView'), children: _jsx(Rows3, { className: "size-4" }) }), _jsx(Button, { variant: view === 'cards' ? 'secondary' : 'ghost', size: "icon", className: "size-7", onClick: () => setViewMode('cards'), "aria-label": t('grid.cardView'), children: _jsx(LayoutGrid, { className: "size-4" }) })] })), viewKey && (_jsx(SavedViews, { viewKey: viewKey, current: { ...grid.state, columnVisibility: tableColumnVisibility }, onApply: (v) => {
                                    grid.reset(v);
                                    setTableColumnVisibility(v.columnVisibility ?? initialColumnVisibility ?? {});
                                    clearSelection();
                                } })), slots.exportMenu, enableColumnHiding && dataColumns.length > 0 && (_jsxs(DropdownMenu, { children: [_jsx(DropdownMenuTrigger, { asChild: true, children: _jsxs(Button, { variant: "outline", size: "sm", children: [_jsx(SlidersHorizontal, { className: "size-4" }), _jsx("span", { className: "hidden sm:inline", children: t('grid.columns') })] }) }), _jsxs(DropdownMenuContent, { align: "end", className: "w-44", children: [_jsx(DropdownMenuLabel, { children: t('grid.columns') }), _jsx(DropdownMenuSeparator, {}), dataColumns.map((column) => (_jsx(DropdownMenuCheckboxItem, { checked: column.getIsVisible(), onCheckedChange: (value) => column.toggleVisibility(!!value), onSelect: (e) => e.preventDefault(), children: labelOf(column.columnDef.meta) ?? column.id }, column.id)))] })] }))] })] }), bulkActions && selectedIds.length > 0 && (_jsxs("div", { className: "bg-accent/60 flex items-center gap-3 rounded-lg border px-3 py-2", children: [_jsx("span", { className: "text-sm font-medium", children: t('grid.selected', { count: selectedIds.length }) }), _jsx("div", { className: "flex items-center gap-2", children: bulkActions(selectedIds, clearSelection) }), _jsxs(Button, { variant: "ghost", size: "sm", className: "ms-auto", onClick: clearSelection, children: [_jsx(X, { className: "size-4" }), t('grid.clearSelection')] })] })), grid.isLoading ? (_jsx("div", { className: "rounded-lg border p-4", children: _jsx(LoadingSkeleton, { variant: "table", rows: grid.pageSize > 10 ? 8 : grid.pageSize, cols: columns.length }) })) : grid.isError ? (_jsx("div", { className: "rounded-lg border", children: _jsx(ErrorState, { error: grid.error, onRetry: () => grid.refetch() }) })) : grid.items.length === 0 ? (_jsx("div", { className: "rounded-lg border", children: empty ? (_jsx(EmptyState, { icon: empty.icon, title: empty.title, description: empty.description, action: empty.action })) : (_jsx(EmptyState, { icon: Search, title: t('grid.emptyTitle'), description: t('grid.emptyDesc') })) })) : isCards ? (
            // Card grid (responsive) — replaces both the table and the auto mobile cards.
            cardList('grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4')) : (_jsxs(_Fragment, { children: [_jsx("div", { className: "hidden overflow-hidden rounded-lg border sm:block", children: _jsxs("table", { className: "w-full text-sm", children: [_jsx("thead", { className: "bg-muted/40", children: table.getHeaderGroups().map((hg) => (_jsx("tr", { className: "border-b", children: hg.headers.map((header) => {
                                            const canSort = header.column.getCanSort();
                                            const sorted = header.column.getIsSorted();
                                            return (_jsx("th", { className: "text-muted-foreground h-10 px-3 text-start align-middle font-medium whitespace-nowrap", children: header.isPlaceholder ? null : canSort ? (_jsxs("button", { type: "button", onClick: header.column.getToggleSortingHandler(), className: "hover:text-foreground -ms-1 inline-flex items-center gap-1 rounded px-1 outline-none focus-visible:ring-[3px]", children: [flexRender(header.column.columnDef.header, header.getContext()), sorted === 'asc' ? (_jsx(ArrowUp, { className: "size-3.5" })) : sorted === 'desc' ? (_jsx(ArrowDown, { className: "size-3.5" })) : (_jsx(ChevronsUpDown, { className: "size-3.5 opacity-50" }))] })) : (flexRender(header.column.columnDef.header, header.getContext())) }, header.id));
                                        }) }, hg.id))) }), _jsx("tbody", { children: table.getRowModel().rows.map((row) => (_jsx("tr", { onClick: onRowClick ? () => onRowClick(row.original) : undefined, className: cn('border-b last:border-0 transition-colors', row.getIsSelected() ? 'bg-accent/40' : 'hover:bg-muted/40', onRowClick && 'cursor-pointer'), children: row.getVisibleCells().map((cell) => (_jsx("td", { className: "px-3 py-2.5 align-middle", children: flexRender(cell.column.columnDef.cell, cell.getContext()) }, cell.id))) }, row.id))) })] }) }), _jsx("ul", { className: "space-y-2.5 sm:hidden", children: table.getRowModel().rows.map((row) => {
                            const cells = row.getVisibleCells();
                            const select = cells.find((c) => c.column.id === '__select');
                            const actions = cells.find((c) => c.column.id === '__actions');
                            const data = cells.filter((c) => c.column.id !== '__select' && c.column.id !== '__actions');
                            const [identity, ...rest] = data;
                            return (_jsxs("li", { onClick: onRowClick ? () => onRowClick(row.original) : undefined, className: cn('flex flex-col gap-2.5 rounded-lg border p-3', row.getIsSelected() && 'ring-primary/40 ring-2', onRowClick && 'cursor-pointer'), children: [_jsxs("div", { className: "flex items-start justify-between gap-2", children: [_jsxs("div", { className: "flex min-w-0 items-start gap-2", children: [select && (_jsx("div", { onClick: (e) => e.stopPropagation(), children: flexRender(select.column.columnDef.cell, select.getContext()) })), identity && (_jsx("div", { className: "min-w-0", children: flexRender(identity.column.columnDef.cell, identity.getContext()) }))] }), actions && (_jsx("div", { className: "-mt-1 -me-1 shrink-0", onClick: (e) => e.stopPropagation(), children: flexRender(actions.column.columnDef.cell, actions.getContext()) }))] }), rest.length > 0 && (_jsx("dl", { className: "grid grid-cols-2 gap-x-4 gap-y-2 border-t pt-2.5", children: rest.map((cell) => (_jsxs("div", { className: "flex min-w-0 flex-col gap-0.5", children: [_jsx("dt", { className: "text-muted-foreground text-xs", children: labelOf(cell.column.columnDef.meta) ?? cell.column.id }), _jsx("dd", { className: "truncate text-sm", children: flexRender(cell.column.columnDef.cell, cell.getContext()) })] }, cell.id))) }))] }, row.id));
                        }) })] })), info && info.totalItems > 0 && (_jsxs("div", { className: "flex flex-wrap items-center justify-between gap-3 px-1", children: [_jsx("p", { className: "text-muted-foreground text-sm", children: t('grid.rangeOf', {
                            from: (info.page - 1) * info.pageSize + 1,
                            to: Math.min(info.page * info.pageSize, info.totalItems),
                            total: info.totalItems,
                        }) }), _jsxs("div", { className: "flex items-center gap-2", children: [_jsxs(DropdownMenu, { children: [_jsx(DropdownMenuTrigger, { asChild: true, children: _jsxs(Button, { variant: "outline", size: "sm", children: [t('grid.perPage', { n: grid.pageSize }), _jsx(ChevronsUpDown, { className: "size-3.5 opacity-60" })] }) }), _jsx(DropdownMenuContent, { align: "end", children: PAGE_SIZES.map((size) => (_jsx(DropdownMenuItem, { onClick: () => grid.setPageSize(size), children: t('grid.perPage', { n: size }) }, size))) })] }), _jsx("span", { className: "text-muted-foreground text-sm tabular-nums", children: t('grid.pageOf', { page: info.page, total: Math.max(info.totalPages, 1) }) }), _jsx(Button, { variant: "outline", size: "icon", className: "size-8", disabled: !info.hasPrev, onClick: () => grid.setPage(info.page - 1), "aria-label": t('grid.prev'), children: _jsx(ChevronLeft, { className: "size-4" }) }), _jsx(Button, { variant: "outline", size: "icon", className: "size-8", disabled: !info.hasNext, onClick: () => grid.setPage(info.page + 1), "aria-label": t('grid.next'), children: _jsx(ChevronRight, { className: "size-4" }) })] })] }))] }));
}
// --- Search with debounce ---
function SearchBox({ value, onChange, placeholder }) {
    const [local, setLocal] = useState(value);
    // Adopt an external value change (e.g. applying a saved view) during render — no effect needed.
    const [synced, setSynced] = useState(value);
    if (value !== synced) {
        setSynced(value);
        setLocal(value);
    }
    // Push the debounced value up. onChange fires only inside the timeout (never synchronously), so
    // this doesn't trip the no-setState-in-effect rule; the guard avoids a redundant page reset.
    useEffect(() => {
        const id = setTimeout(() => {
            if (local !== value)
                onChange(local);
        }, 300);
        return () => clearTimeout(id);
    }, [local, value, onChange]);
    return (_jsxs("div", { className: "relative w-full sm:w-64", children: [_jsx(Search, { className: "text-muted-foreground pointer-events-none absolute start-2.5 top-1/2 size-4 -translate-y-1/2" }), _jsx(Input, { value: local, onChange: (e) => setLocal(e.target.value), placeholder: placeholder, className: "ps-8" })] }));
}
function SavedViews({ viewKey, current, onApply, }) {
    const { t } = useTranslation();
    const storageKey = viewsStorageKey(viewKey);
    const [views, setViews] = useState(() => readViews(storageKey));
    const persist = (next) => {
        setViews(next);
        localStorage.setItem(storageKey, JSON.stringify(next));
    };
    const save = () => {
        const name = window.prompt(t('grid.viewName'))?.trim();
        if (!name)
            return;
        persist([...views.filter((v) => v.name !== name), { name, state: current }]);
    };
    // Single-select: starring a view clears any other default; starring the current default clears it.
    const toggleDefault = (name) => persist(views.map((v) => ({ ...v, isDefault: v.name === name ? !v.isDefault : false })));
    return (_jsxs(DropdownMenu, { children: [_jsx(DropdownMenuTrigger, { asChild: true, children: _jsx(Button, { variant: "outline", size: "sm", children: t('grid.views') }) }), _jsxs(DropdownMenuContent, { align: "end", className: "w-60", children: [_jsx(DropdownMenuLabel, { children: t('grid.savedViews') }), _jsx(DropdownMenuSeparator, {}), views.length === 0 ? (_jsx("p", { className: "text-muted-foreground px-2 py-1.5 text-xs", children: t('grid.noViews') })) : (views.map((view) => (_jsxs("div", { className: "flex items-center", children: [_jsx(Button, { variant: "ghost", size: "icon", className: cn('ms-1 size-7', view.isDefault ? 'text-primary' : 'text-muted-foreground'), onClick: () => toggleDefault(view.name), "aria-label": t('grid.setDefaultView'), "aria-pressed": !!view.isDefault, title: t('grid.setDefaultView'), children: _jsx(Star, { className: cn('size-3.5', view.isDefault && 'fill-current') }) }), _jsx(DropdownMenuItem, { className: "flex-1", onClick: () => onApply(view.state), children: view.name }), _jsx(Button, { variant: "ghost", size: "icon", className: "text-muted-foreground hover:text-destructive me-1 size-7", onClick: () => persist(views.filter((v) => v.name !== view.name)), "aria-label": t('grid.deleteView'), children: _jsx(X, { className: "size-3.5" }) })] }, view.name)))), _jsx(DropdownMenuSeparator, {}), _jsx(DropdownMenuItem, { onClick: save, children: t('grid.saveCurrentView') })] })] }));
}
const viewsStorageKey = (viewKey) => `nexaecommerce:grid-views:${viewKey}`;
function readViews(key) {
    try {
        const raw = localStorage.getItem(key);
        return raw ? JSON.parse(raw) : [];
    }
    catch {
        return [];
    }
}
/** The pinned default view for a grid, if one is set. */
function readDefaultView(viewKey) {
    return readViews(viewsStorageKey(viewKey)).find((v) => v.isDefault);
}
/** True when the page opened with no grid query string — so applying a default view is safe. */
function isBareUrl() {
    return typeof window !== 'undefined' && window.location.search.replace(/^\?/, '') === '';
}
function labelOf(meta) {
    return meta && typeof meta === 'object' && 'label' in meta ? String(meta.label) : undefined;
}
