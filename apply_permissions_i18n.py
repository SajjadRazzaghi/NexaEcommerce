#!/usr/bin/env python3
"""Apply inventory-section i18n fixes to NexaECommerce from the repository root."""
from __future__ import annotations

import json
import re
import sys
from pathlib import Path

TRANSLATIONS = {
    "en": {
        "group": "Inventory",
        "read": "View inventory",
        "manage": "Manage inventory",
    },
    "fa": {
        "group": "موجودی",
        "read": "مشاهده موجودی انبار",
        "manage": "مدیریت موجودی انبار",
    },
    "es": {
        "group": "Inventario",
        "read": "Consultar el inventario",
        "manage": "Gestionar el inventario",
    },
    "fr": {
        "group": "Stock",
        "read": "Consulter le stock",
        "manage": "Gérer le stock",
    },
    "de": {
        "group": "Lagerbestand",
        "read": "Lagerbestand anzeigen",
        "manage": "Lagerbestand verwalten",
    },
    "ar": {
        "group": "المخزون",
        "read": "عرض المخزون",
        "manage": "إدارة المخزون",
    },
    "zh": {
        "group": "库存",
        "read": "查看库存",
        "manage": "管理库存",
    },
}

PAGE = Path("nexaecommerce.client/src/pages/(app)/admin/permissions/index.tsx")
LOCALE_DIR = Path("nexaecommerce.client/src/locales")


def matching_brace(text: str, start: int) -> int:
    depth = 0
    in_string = False
    escaped = False
    for index in range(start, len(text)):
        char = text[index]
        if in_string:
            if escaped:
                escaped = False
            elif char == "\\":
                escaped = True
            elif char == '"':
                in_string = False
            continue
        if char == '"':
            in_string = True
        elif char == "{":
            depth += 1
        elif char == "}":
            depth -= 1
            if depth == 0:
                return index
    raise ValueError("Unbalanced braces while locating the permissions locale block.")


def patch_locale(path: Path, lang: str, translation: dict[str, str]) -> bool:
    original = path.read_text(encoding="utf-8")
    document = json.loads(original)
    permissions = document.get("permissions")
    if not isinstance(permissions, dict) or not isinstance(permissions.get("groups"), dict):
        raise ValueError(f"Unexpected permissions translation structure in {path}")

    permissions["groups"].setdefault("inventory", translation["group"])
    descriptions = permissions.setdefault("descriptions", {})
    inventory = descriptions.setdefault("inventory", {})
    inventory["read"] = translation["read"]
    inventory["manage"] = translation["manage"]

    marker = re.search(r'^  "permissions":\s*\{', original, flags=re.MULTILINE)
    if not marker:
        raise ValueError(f"Could not find the top-level permissions object in {path}")
    value_start = original.index("{", marker.start())
    value_end = matching_brace(original, value_start)

    formatted = json.dumps({"permissions": permissions}, ensure_ascii=False, indent=2).splitlines()
    replacement = "\n".join(formatted[1:-1])
    updated = original[: marker.start()] + replacement + original[value_end + 1 :]
    json.loads(updated)  # Validate the entire JSON file before writing.
    if updated != original:
        path.write_text(updated, encoding="utf-8", newline="")
        return True
    return False


def main() -> int:
    if not PAGE.is_file() or not LOCALE_DIR.is_dir():
        print(
            "Run this script from the NexaECommerce repository root; the expected client paths were not found.",
            file=sys.stderr,
        )
        return 2

    page = PAGE.read_text(encoding="utf-8")
    page = page.replace("import { meta } from './meta';\n", "")
    page = page.replace("  useDocumentTitle(meta.title);", "  useDocumentTitle(t('permissions.title'));")

    raw_description = '<span className="text-foreground">{permission.description}</span>'
    localized_description = '''<span className="text-foreground">
                          {t(`permissions.descriptions.${permission.name}`, {
                            defaultValue: permission.description,
                          })}
                        </span>'''
    page = page.replace(raw_description, localized_description)

    if "useDocumentTitle(t('permissions.title'));" not in page:
        raise ValueError("Could not apply the localized document title to the permissions page.")
    if "permissions.descriptions.${permission.name}" not in page:
        raise ValueError("Could not localize permission descriptions on the permissions page.")
    if raw_description in page:
        raise ValueError("A raw permission description remains after patching.")

    changed = []
    for lang, translation in TRANSLATIONS.items():
        locale_path = LOCALE_DIR / f"{lang}.json"
        if not locale_path.is_file():
            raise FileNotFoundError(f"Missing locale file: {locale_path}")
        if patch_locale(locale_path, lang, translation):
            changed.append(str(locale_path))

    PAGE.write_text(page, encoding="utf-8", newline="")
    changed.insert(0, str(PAGE))

    # Final checks against the files written to disk.
    for lang in TRANSLATIONS:
        document = json.loads((LOCALE_DIR / f"{lang}.json").read_text(encoding="utf-8"))
        inventory = document["permissions"]["descriptions"]["inventory"]
        assert inventory["read"] and inventory["manage"]
        assert document["permissions"]["groups"]["inventory"]

    print("Updated files:")
    for file in changed:
        print(f"  {file}")
    print("Validated inventory group labels and inventory.read/inventory.manage translations for all 7 languages.")
    print("Note: run the client build/tests after applying this change.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
