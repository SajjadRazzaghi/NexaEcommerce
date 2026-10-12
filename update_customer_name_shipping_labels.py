#!/usr/bin/env python3
"""Show customer names in the admin orders list and move bulk label printing to fulfillment.

Run this script from the NexaECommerce repository root.
It edits six existing files and creates one-time .bak_customer_name_shipping_labels backups.
The script intentionally aborts before writing if it cannot safely find the expected source patterns.
"""
from __future__ import annotations

import re
import sys
from pathlib import Path

BACKUP_SUFFIX = ".bak_customer_name_shipping_labels"


def fail(message: str) -> None:
    raise RuntimeError(message)


def read_required(root: Path, rel: str) -> str:
    path = root / rel
    if not path.is_file():
        fail(f"Required file not found: {path}")
    return path.read_text(encoding="utf-8-sig")


def replace_once_or_done(
    text: str,
    old: str,
    new: str,
    *,
    path: str,
    done_marker: str,
) -> str:
    count = text.count(old)
    if count == 1:
        return text.replace(old, new, 1)
    if count == 0 and done_marker in text:
        return text
    fail(f"Expected exactly one source pattern in {path}; found {count}. Pattern prefix: {old[:80]!r}. No files were changed.")


def regex_once_or_done(
    text: str,
    pattern: str,
    replacement: str,
    *,
    path: str,
    done_marker: str,
    flags: int = re.MULTILINE | re.DOTALL,
) -> str:
    text2, count = re.subn(pattern, replacement, text, count=1, flags=flags)
    if count == 1:
        return text2
    if done_marker in text:
        return text
    fail(f"Could not find the expected source block in {path}. No files were changed.")


def ensure_absent_or_remove(text: str, old: str, *, path: str, absent_marker: str) -> str:
    if old in text:
        return text.replace(old, "", 1)
    if absent_marker in text:
        return text
    fail(f"Could not find the expected block to remove in {path}. No files were changed.")


def main() -> int:
    cwd = Path.cwd()
    repo_candidates = [cwd, Path(__file__).resolve().parent]
    root = next(
        (candidate for candidate in repo_candidates
         if (candidate / "nexaecommerce.client").is_dir()
         and (candidate / "NexaEcommerce.Modules").is_dir()),
        None,
    )
    if root is None:
        print(
            "Run this script from the NexaECommerce repository root, where "
            "nexaecommerce.client and NexaEcommerce.Modules folders exist.",
            file=sys.stderr,
        )
        return 2

    files = {
        "dto": "NexaEcommerce.Modules/NexaEcommerce.Modules.Orders/Application/DTOs/OrderDtos.cs",
        "service": "NexaEcommerce.Modules/NexaEcommerce.Modules.Orders/Application/Services/OrderService.cs",
        "types": "nexaecommerce.client/src/modules/orders/types.ts",
        "orders_page": "nexaecommerce.client/src/pages/(app)/admin/orders/index.tsx",
        "fulfillment_page": "nexaecommerce.client/src/pages/(app)/admin/fulfillment/index.tsx",
        "order_details_page": "nexaecommerce.client/src/pages/(app)/admin/orders/[id]/index.tsx",
    }

    # Read and patch everything in memory first. This prevents partial edits if a pattern mismatches.
    contents = {key: read_required(root, rel) for key, rel in files.items()}

    # 1) Add shipping recipient's display name to the admin list DTO.
    p = files["dto"]
    contents["dto"] = replace_once_or_done(
        contents["dto"],
        """public sealed record OrderListItemDto(
    Guid Id,
    string OrderNumber,
    string UserId,
    string Status,""",
        """public sealed record OrderListItemDto(
    Guid Id,
    string OrderNumber,
    string UserId,
    string ShippingFullName,
    string Status,""",
        path=p,
        done_marker="string ShippingFullName,\n    string Status,",
    )

    # 2) Map the value from the existing Order aggregate into the DTO.
    p = files["service"]
    contents["service"] = replace_once_or_done(
        contents["service"],
        """                        new OrderListItemDto(
                            x.Id,
                            x.OrderNumber,
                            x.UserId,
                            x.Status.ToString(),""",
        """                        new OrderListItemDto(
                            x.Id,
                            x.OrderNumber,
                            x.UserId,
                            x.ShippingFullName,
                            x.Status.ToString(),""",
        path=p,
        done_marker="new OrderListItemDto(\n                            x.Id,\n                            x.OrderNumber,\n                            x.UserId,\n                            x.ShippingFullName,",
    )

    # 3) Keep the front-end API shape aligned with the backend.
    p = files["types"]
    contents["types"] = replace_once_or_done(
        contents["types"],
        """export interface OrderListItemDto {
    id: string;
    orderNumber: string;
    userId: string;
    status: OrderStatus;""",
        """export interface OrderListItemDto {
    id: string;
    orderNumber: string;
    userId: string;
    shippingFullName: string;
    status: OrderStatus;""",
        path=p,
        done_marker="export interface OrderListItemDto {\n    id: string;\n    orderNumber: string;\n    userId: string;\n    shippingFullName: string;",
    )

    # 4) Remove the label-print workflow from the general orders page.
    p = files["orders_page"]
    s = contents["orders_page"]
    s = s.replace("        Printer,\n", "", 1) if "        Printer,\n" in s else s

    # Replace the technical user UUID with the recipient name before removing checkboxes.
    s = replace_once_or_done(
        s,
        """                                                <td className=\"px-4 py-4\">
                                                    <div className=\"max-w-[220px] truncate\">
                                                        {
                                                            order.userId
                                                        }
                                                    </div>
                                                </td>""",
        """                                                <td className=\"px-4 py-4\">
                                                    <div className=\"max-w-[240px] truncate font-medium\" title={order.shippingFullName}>
                                                        {order.shippingFullName?.trim() || '—'}
                                                    </div>
                                                </td>""",
        path=p,
        done_marker="order.shippingFullName?.trim()",
    )

    # The selection state was used only for printing labels on this page.
    if "selectedOrderIds" in s and "const visibleOrderIds =" in s:
        s = regex_once_or_done(
            s,
            r"[ \t]*const \[\s*selectedOrderIds,[\s]*setSelectedOrderIds,[\s]*\][\s]*=[\s]*useState<Set<string>>\([\s]*\(\) => new Set\(\),[\s]*\);[ \t]*\n",
            "",
            path=p,
            done_marker="const visibleOrderIds =",
        )
    s = regex_once_or_done(
        s,
        r"    const visibleOrderIds =[\s\S]*?(?=    const text = \{)",
        "",
        path=p,
        done_marker="const visibleOrderIds =",
    ) if "const visibleOrderIds =" in s else s

    # Remove text keys that were exclusive to the old label toolbar/checkboxes.
    for old in [
        """        selected: t(
            'storefront.orders.selected',
        ),

""",
        """        printLabels: t(
            'storefront.orders.printLabels',
        ),

""",
        """        selectAll: t(
            'storefront.orders.selectAll',
        ),

""",
    ]:
        if old in s:
            s = s.replace(old, "", 1)

    s = regex_once_or_done(
        s,
        r"            <section className=\"flex flex-wrap items-center justify-between gap-3 rounded-xl border p-4\">\s*"
        r"<div className=\"text-sm text-muted-foreground\">\s*\{selectedOrderIds\.size\} \{text\.selected\}\s*</div>\s*"
        r"<button[\s\S]*?<Printer className=\"size-4\" />\s*\{text\.printLabels\}\s*</button>\s*</section>\s*",
        "",
        path=p,
        done_marker="printSelectedOrders",
    ) if "selectedOrderIds" in s else s

    # Remove the select-all column from the order list.
    s = regex_once_or_done(
        s,
        r"\s*<th className=\"w-12 px-4 py-3 text-center\">\s*<input\s+type=\"checkbox\"[\s\S]*?className=\"size-4 rounded border\"\s*/>\s*</th>\s*",
        "\n",
        path=p,
        done_marker="aria-label={text.selectAll}",
    ) if "text.selectAll" in s else s

    # Remove the per-order checkbox cell.
    s = regex_once_or_done(
        s,
        r"\s*<td className=\"w-12 px-4 py-4 text-center align-top\">\s*<input\s+type=\"checkbox\"[\s\S]*?className=\"mt-1 size-4 rounded border\"\s*/>\s*</td>\s*",
        "\n",
        path=p,
        done_marker="selectedOrderIds.has(",
    ) if "selectedOrderIds.has(" in s else s

    contents["orders_page"] = s

    # 5) Add safe bulk label printing to the fulfillment queue, only selectable at ReadyToShip.
    p = files["fulfillment_page"]
    s = contents["fulfillment_page"]
    if "        Printer,\n" not in s:
        s = replace_once_or_done(
            s,
            """    Package,
    RefreshCw,""",
            """    Package,
    Printer,
    RefreshCw,""",
            path=p,
            done_marker="    Printer,",
        )

    state_block = """    const [
        selectedOrderIds,
        setSelectedOrderIds,
    ] = useState<Set<string>>(
        () => new Set(),
    );

"""
    s = replace_once_or_done(
        s,
        """    const fulfillmentItems =
        data?.items ?? [];

    const text = isFa""",
        """    const fulfillmentItems =
        data?.items ?? [];

""" + state_block + """    const readyToShipItems =
        fulfillmentItems.filter(
            item => item.fulfillment.status === 'ReadyToShip',
        );

    const allReadyToShipSelected =
        readyToShipItems.length > 0 &&
        readyToShipItems.every(
            item => selectedOrderIds.has(item.orderId),
        );

    const toggleOrderSelection = (orderId: string) => {
        setSelectedOrderIds(current => {
            const next = new Set(current);
            if (next.has(orderId)) {
                next.delete(orderId);
            } else {
                next.add(orderId);
            }
            return next;
        });
    };

    const toggleVisibleReadyToShipSelection = () => {
        setSelectedOrderIds(current => {
            const next = new Set(current);
            if (allReadyToShipSelected) {
                readyToShipItems.forEach(item => next.delete(item.orderId));
            } else {
                readyToShipItems.forEach(item => next.add(item.orderId));
            }
            return next;
        });
    };

    const printSelectedLabels = () => {
        if (selectedOrderIds.size === 0) return;
        const ids = Array.from(selectedOrderIds).join(',');
        window.open(
            `/admin/orders/shipping-labels?ids=${encodeURIComponent(ids)}`,
            '_blank',
            'noopener,noreferrer',
        );
    };

    const text = isFa""",
        path=p,
        done_marker="const printSelectedLabels = () =>",
    )

    # Add labels to this page's existing Persian/English literal text blocks.
    s = replace_once_or_done(
        s,
        """            warehouseNotFound:
                'انبار یافت نشد',
        }
        : {""",
        """            warehouseNotFound:
                'انبار یافت نشد',
            selected:
                'سفارش انتخاب‌شده',
            printLabels:
                'چاپ برچسب‌های ارسال',
            selectReadyToShip:
                'انتخاب سفارش آماده ارسال',
        }
        : {""",
        path=p,
        done_marker="'چاپ برچسب‌های ارسال'",
    )
    s = replace_once_or_done(
        s,
        """            warehouseNotFound:
                'Warehouse not found',
        };""",
        """            warehouseNotFound:
                'Warehouse not found',
            selected:
                'selected',
            printLabels:
                'Print shipping labels',
            selectReadyToShip:
                'Select a ready-to-ship order',
        };""",
        path=p,
        done_marker="'Print shipping labels'",
    )

    # Put the bulk-print control next to fulfillment operations, not general order administration.
    s = replace_once_or_done(
        s,
        """            </header>

            {isLoading && (""",
        """            </header>

            <section className=\"flex flex-wrap items-center justify-between gap-3 rounded-xl border p-4\">
                <div className=\"text-sm text-muted-foreground\">
                    {selectedOrderIds.size} {text.selected}
                </div>
                <button
                    type=\"button\"
                    disabled={selectedOrderIds.size === 0}
                    onClick={printSelectedLabels}
                    className=\"inline-flex items-center gap-2 rounded-lg bg-primary px-4 py-2.5 text-sm font-medium text-primary-foreground disabled:cursor-not-allowed disabled:opacity-50\"
                >
                    <Printer className=\"size-4\" />
                    {text.printLabels}
                </button>
            </section>

            {isLoading && (""",
        path=p,
        done_marker="onClick={printSelectedLabels}",
    )

    # Add select-all checkbox in the fulfillment table; only ReadyToShip rows are selectable.
    s = replace_once_or_done(
        s,
        """                                    <tr className=\"border-b bg-muted/30\">
                                        <th className=\"px-4 py-3 text-start font-medium\">
                                            {text.order}
                                        </th>""",
        """                                    <tr className=\"border-b bg-muted/30\">
                                        <th className=\"w-12 px-4 py-3 text-center\">
                                            <input
                                                type=\"checkbox\"
                                                checked={allReadyToShipSelected}
                                                onChange={toggleVisibleReadyToShipSelection}
                                                disabled={readyToShipItems.length === 0}
                                                aria-label={text.selectReadyToShip}
                                                title={text.selectReadyToShip}
                                                className=\"size-4 rounded border\"
                                            />
                                        </th>
                                        <th className=\"px-4 py-3 text-start font-medium\">
                                            {text.order}
                                        </th>""",
        path=p,
        done_marker="checked={allReadyToShipSelected}",
    )

    s = replace_once_or_done(
        s,
        """                                                >
                                                    <td className=\"px-4 py-4\">
                                                        <Link
                                                            to={`/admin/orders/${item.orderId}`}""",
        """                                                >
                                                    <td className=\"w-12 px-4 py-4 text-center\">
                                                        <input
                                                            type=\"checkbox\"
                                                            checked={selectedOrderIds.has(item.orderId)}
                                                            disabled={item.fulfillment.status !== 'ReadyToShip'}
                                                            onChange={() => toggleOrderSelection(item.orderId)}
                                                            aria-label={`${text.selectReadyToShip}: ${item.orderNumber}`}
                                                            className=\"size-4 rounded border disabled:opacity-30\"
                                                        />
                                                    </td>
                                                    <td className=\"px-4 py-4\">
                                                        <Link
                                                            to={`/admin/orders/${item.orderId}`}""",
        path=p,
        done_marker="checked={selectedOrderIds.has(item.orderId)}",
    )
    contents["fulfillment_page"] = s

    # 6) Keep single-order label printing/reprinting beside shipment operations.
    p = files["order_details_page"]
    s = contents["order_details_page"]
    s = replace_once_or_done(
        s,
        """    Package,
    Truck,""",
        """    Package,
    Printer,
    Truck,""",
        path=p,
        done_marker="    Printer,",
    )
    s = replace_once_or_done(
        s,
        """            shipmentConfig:
                'مدیریت روش‌های ارسال',

            total:""",
        """            shipmentConfig:
                'مدیریت روش‌های ارسال',

            printLabel:
                'چاپ لیبل ارسال',

            total:""",
        path=p,
        done_marker="'چاپ لیبل ارسال'",
    )
    s = replace_once_or_done(
        s,
        """            shipmentConfig:
                'Manage shipping methods',

            total:""",
        """            shipmentConfig:
                'Manage shipping methods',

            printLabel:
                'Print shipping label',

            total:""",
        path=p,
        done_marker="'Print shipping label'",
    )
    s = replace_once_or_done(
        s,
        """                                </div>


                                {/* ================================================= */}
                                {/* Tracking */}
                                {/* ================================================= */}""",
        """                                </div>

                                <div className="flex justify-end">
                                    <button
                                        type="button"
                                        onClick={() =>
                                            window.open(
                                                `/admin/orders/shipping-labels?ids=${encodeURIComponent(order.id)}`,
                                                '_blank',
                                                'noopener,noreferrer',
                                            )
                                        }
                                        className="inline-flex items-center gap-2 rounded-lg border px-4 py-2.5 text-sm font-medium"
                                    >
                                        <Printer className="size-4" />
                                        {text.printLabel}
                                    </button>
                                </div>

                                {/* ================================================= */}
                                {/* Tracking */}
                                {/* ================================================= */}""",
        path=p,
        done_marker="window.open(",
    )
    contents["order_details_page"] = s

    # Ensure the customer display is no longer the UUID in the order listing.
    if "order.userId" in contents["orders_page"]:
        fail("The orders table still references order.userId; refusing to write incomplete changes.")
    if "order.shippingFullName" not in contents["orders_page"]:
        fail("The customer name is not displayed in the orders table; refusing to write incomplete changes.")
    if "printSelectedOrders" in contents["orders_page"] or "selectedOrderIds" in contents["orders_page"]:
        fail("The old bulk label action was not fully removed from the orders page.")
    if "printSelectedLabels" not in contents["fulfillment_page"]:
        fail("The fulfillment label action was not inserted correctly.")
    if "text.printLabel" not in contents["order_details_page"] or "Printer" not in contents["order_details_page"]:
        fail("The single-order reprint action was not inserted correctly.")
    if "x.ShippingFullName" not in contents["service"] or "string ShippingFullName" not in contents["dto"]:
        fail("The API response changes are incomplete.")

    # Back up originals and write only after all validation passes.
    for key, rel in files.items():
        path = root / rel
        backup = path.with_name(path.name + BACKUP_SUFFIX)
        if not backup.exists():
            backup.write_text(path.read_text(encoding="utf-8-sig"), encoding="utf-8", newline="")
        path.write_text(contents[key], encoding="utf-8", newline="")

    print("Updated successfully:")
    for rel in files.values():
        print(f"  - {root / rel}")
    print(f"Backups were saved next to each edited file with suffix {BACKUP_SUFFIX}.")
    print("Next: run npm run build in nexaecommerce.client, then dotnet build the server solution.")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except RuntimeError as exc:
        print(f"ERROR: {exc}", file=sys.stderr)
        raise SystemExit(1)
