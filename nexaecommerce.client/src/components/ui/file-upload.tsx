// src/components/ui/file-upload.tsx
import React, { useState, useRef } from 'react';
import { useTranslation } from 'react-i18next';
import { Upload, X, Loader2 } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { cn } from '@/lib/utils';

interface FileUploadProps {
    value?: string;
    onChange: (url: string) => void;
    onRemove?: () => void;
    accept?: string;
    maxSize?: number;
    className?: string;
    label?: string;
    placeholder?: string;
}

export function FileUpload({
    value,
    onChange,
    onRemove,
    accept = 'image/*',
    maxSize = 5,
    className,
    // label = 'آپلود فایل',
    placeholder,
}: FileUploadProps) {
    const { t } = useTranslation();
    const uploadPlaceholder = placeholder ?? t('fileUpload.placeholder');
    const [isUploading, setIsUploading] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const [preview, setPreview] = useState<string | null>(value || null);
    const fileInputRef = useRef<HTMLInputElement>(null);

    // ✅ ساخت URL کامل برای نمایش تصویر
    const getImageUrl = (url: string) => {
        // اگر URL با http شروع شود، همان است
        if (url.startsWith('http')) return url;
        // اگر URL با / شروع شود، از پروکسی استفاده کن
        if (url.startsWith('/')) return url;
        // در غیر این صورت، / را اضافه کن
        return `/${url}`;
    };

    const handleFileChange = async (event: React.ChangeEvent<HTMLInputElement>) => {
        const file = event.target.files?.[0];
        if (!file) return;

        if (file.size > maxSize * 1024 * 1024) {
            setError(t('fileUpload.maxSize', { maxSize }));
            return;
        }

        if (!file.type.startsWith('image/')) {
            setError(t('fileUpload.imageOnly'));
            return;
        }

        setError(null);
        setIsUploading(true);

        try {
            const formData = new FormData();
            formData.append('file', file);

            const uploadUrl = '/api/uploads';
            console.log('Uploading to:', uploadUrl);

            const response = await fetch(uploadUrl, {
                method: 'POST',
                body: formData,
                credentials: 'include',
            });

            if (!response.ok) {
                const errorText = await response.text();
                console.error('Upload error response:', errorText);
                throw new Error(t('fileUpload.uploadError', { status: response.status }));
            }

            const data = await response.json();
            console.log('Upload response:', data);

            if (!data.url) {
                throw new Error(t('fileUpload.invalidResponse'));
            }

            // ✅ ذخیره URL
            const fileUrl = data.url;
            setPreview(fileUrl);
            onChange(fileUrl);
            setError(null);
        } catch (err) {
            console.error('Upload error:', err);
            setError(err instanceof Error ? err.message : t('fileUpload.uploadFailed'));
        } finally {
            setIsUploading(false);
        }
    };

    const handleRemove = () => {
        setPreview(null);
        onChange('');
        onRemove?.();
        if (fileInputRef.current) {
            fileInputRef.current.value = '';
        }
    };

    const handleClick = () => {
        fileInputRef.current?.click();
    };

    return (
        <div className={cn('space-y-2', className)}>
            <div
                className={cn(
                    'relative border-2 border-dashed rounded-lg p-6',
                    'transition-colors duration-200',
                    'hover:border-primary/50',
                    error ? 'border-red-500' : 'border-gray-300',
                    'cursor-pointer'
                )}
                onClick={handleClick}
            >
                <input
                    ref={fileInputRef}
                    type="file"
                    accept={accept}
                    onChange={handleFileChange}
                    className="hidden"
                    disabled={isUploading}
                />

                {isUploading ? (
                    <div className="flex flex-col items-center justify-center py-4">
                        <Loader2 className="w-10 h-10 animate-spin text-primary" />
                        <p className="mt-2 text-sm text-gray-500">{t('fileUpload.uploading')}</p>
                    </div>
                ) : preview ? (
                    <div className="relative">
                        <img
                            src={getImageUrl(preview)} // ✅ استفاده از تابع کمکی
                            alt="Preview"
                            className="max-h-48 w-auto mx-auto rounded-lg object-contain"
                            onError={(e) => {
                                console.error('Image load error:', preview);
                                // ✅ اگر خطا داشت، با URL کامل تلاش کن
                                const target = e.target as HTMLImageElement;
                                if (!target.src.startsWith('http://localhost:5000')) {
                                    target.src = `http://localhost:5000${preview}`;
                                }
                            }}
                        />
                        <button
                            type="button"
                            onClick={(e) => {
                                e.stopPropagation();
                                handleRemove();
                            }}
                            className="absolute -top-2 -right-2 p-1 bg-red-500 text-white rounded-full hover:bg-red-600 transition-colors"
                        >
                            <X className="w-4 h-4" />
                        </button>
                    </div>
                ) : (
                    <div className="flex flex-col items-center justify-center py-4 text-gray-500">
                        <Upload className="w-10 h-10 mb-2" />
                        <p className="text-sm">{uploadPlaceholder}</p>
                        <p className="text-xs mt-1">
                            {accept === 'image/*' ? t('fileUpload.images') : accept} • {t('fileUpload.maxSizeHint', { maxSize })}
                        </p>
                        <Button
                            type="button"
                            variant="outline"
                            size="sm"
                            className="mt-3"
                            onClick={(e) => {
                                e.stopPropagation();
                                handleClick();
                            }}
                        >
                            {t('fileUpload.chooseFile')}
                        </Button>
                    </div>
                )}
            </div>

            {error && <p className="text-sm text-red-500">{error}</p>}
        </div>
    );
}