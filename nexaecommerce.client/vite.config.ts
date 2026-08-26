// vite.config.ts
import { fileURLToPath, URL } from 'node:url';
import { defineConfig } from 'vite';
import plugin from '@vitejs/plugin-react';
import tailwindcss from '@tailwindcss/vite';
import generouted from '@generouted/react-router/plugin';
import fs from 'fs';
import path from 'path';
import child_process from 'child_process';
import { env } from 'process';

// ============================================
// 1. تنظیم گواهی SSL
// ============================================
const baseFolder =
    env.APPDATA !== undefined && env.APPDATA !== ''
        ? `${env.APPDATA}/ASP.NET/https`
        : `${env.HOME}/.aspnet/https`;

const certificateName = "nexaecommerce.client";
const certFilePath = path.join(baseFolder, `${certificateName}.pem`);
const keyFilePath = path.join(baseFolder, `${certificateName}.key`);

if (!fs.existsSync(baseFolder)) {
    fs.mkdirSync(baseFolder, { recursive: true });
}

if (!fs.existsSync(certFilePath) || !fs.existsSync(keyFilePath)) {
    if (0 !== child_process.spawnSync('dotnet', [
        'dev-certs',
        'https',
        '--export-path',
        certFilePath,
        '--format',
        'Pem',
        '--no-password',
    ], { stdio: 'inherit', }).status) {
        throw new Error("Could not create certificate.");
    }
}

// ✅ استفاده از HTTP برای راحتی
// ============================================
// 2. تنظیم Target (هماهنگ با Backend)
// ============================================
const target = 'https://localhost:5001';

// ============================================
// 3. تنظیمات Vite
// ============================================
export default defineConfig(({ mode }) => ({
    define: {
        'process.env.DRAGGABLE_DEBUG': 'false',
        'process.env.NODE_ENV': JSON.stringify(mode),
    },
    plugins: [
        plugin(),
        tailwindcss(),
        generouted(),
    ],
    resolve: {
        alias: {
            '@': fileURLToPath(new URL('./src', import.meta.url))
        }
    },
    server: {
        proxy: {
            '^/api': {
                target,
                secure: false,
                changeOrigin: true,
                ws: true,
            },

            '/uploads': {
                target,
                changeOrigin: true,
                secure: false,
            },

            '^/hubs': {
                target,
                secure: false,
                changeOrigin: true,
                ws: true,
            },

            '^/scalar': {
                target,
                secure: false,
                changeOrigin: true,
            },

            '^/openapi': {
                target,
                secure: false,
                changeOrigin: true,
            },

            '^/hangfire': {
                target,
                secure: false,
                changeOrigin: true,
            }
        },

        port: parseInt(env.DEV_SERVER_PORT || '3000'),

        https: {
            key: fs.readFileSync(keyFilePath),
            cert: fs.readFileSync(certFilePath),
        }
    },
    // ✅ اضافه کردن برای حل مشکل کش
    optimizeDeps: {
        include: ['@/lib/utils'],
        force: true,
    },
}));