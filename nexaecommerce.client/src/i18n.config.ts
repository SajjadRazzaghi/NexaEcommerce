import i18n from 'i18next';
import { initReactI18next } from 'react-i18next';
import LanguageDetector from 'i18next-browser-languagedetector';

import en from './locales/en.json';
import es from './locales/es.json';
import fr from './locales/fr.json';
import de from './locales/de.json';
import ar from './locales/ar.json';
import zh from './locales/zh.json';
import fa from './locales/fa.json';

import warehouseEn from './locales/warehouse.en';
import warehouseFa from './locales/warehouse.fa';

import inventoryEn from './locales/inventory.en';
import inventoryFa from './locales/inventory.fa';

import shippingEn from './locales/shipping.en';
import shippingFa from './locales/shipping.fa';

export type LanguageMeta = {
    code: string;
    name: string;
    dir: 'ltr' | 'rtl';
};

export const LANGUAGES: LanguageMeta[] = [
    {
        code: 'fa',
        name: 'فارسی',
        dir: 'rtl',
    },
    {
        code: 'en',
        name: 'English',
        dir: 'ltr',
    },
    {
        code: 'es',
        name: 'Español',
        dir: 'ltr',
    },
    {
        code: 'fr',
        name: 'Français',
        dir: 'ltr',
    },
    {
        code: 'de',
        name: 'Deutsch',
        dir: 'ltr',
    },
    {
        code: 'ar',
        name: 'العربية',
        dir: 'rtl',
    },
    {
        code: 'zh',
        name: '中文',
        dir: 'ltr',
    },
];

export const supportedLngs =
    LANGUAGES.map(
        language =>
            language.code,
    );

export function directionOf(
    code: string,
): 'ltr' | 'rtl' {
    return (
        LANGUAGES.find(
            language =>
                language.code === code,
        )?.dir ?? 'ltr'
    );
}

i18n
    .use(LanguageDetector)
    .use(initReactI18next)
    .init({
        resources: {
            en: {
                translation: {
                    ...en,

                    nav: {
                        ...en.nav,
                        ...warehouseEn.nav,
                        ...inventoryEn.nav,
                        ...shippingEn.nav,
                    },

                    warehouses:
                        warehouseEn.warehouses,

                    inventory:
                        inventoryEn.inventory,

                    shipping:
                        shippingEn.shipping,
                },
            },

            fa: {
                translation: {
                    ...fa,

                    nav: {
                        ...fa.nav,
                        ...warehouseFa.nav,
                        ...inventoryFa.nav,
                        ...shippingFa.nav,
                    },

                    warehouses:
                        warehouseFa.warehouses,

                    inventory:
                        inventoryFa.inventory,

                    shipping:
                        shippingFa.shipping,
                },
            },

            es: {
                translation: es,
            },

            fr: {
                translation: fr,
            },

            de: {
                translation: de,
            },

            ar: {
                translation: ar,
            },

            zh: {
                translation: zh,
            },
        },

        fallbackLng: 'fa',

        supportedLngs,

        nonExplicitSupportedLngs:
            true,

        interpolation: {
            escapeValue: false,
        },

        detection: {
            order: [
                'localStorage',
            ],

            caches: [
                'localStorage',
            ],

            lookupLocalStorage:
                'nexaecommerce-lang',
        },
    });

export default i18n;