import React, {
    useEffect,
    useRef,
    useState,
} from 'react';

import {
    useTranslation,
} from 'react-i18next';

import {
    Upload,
    X,
    Loader2,
} from 'lucide-react';

import {
    Button,
} from '@/components/ui/button';

import {
    cn,
} from '@/lib/utils';

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
    placeholder,
}: FileUploadProps) {
    const { t } =
        useTranslation();

    const uploadPlaceholder =
        placeholder ??
        t('fileUpload.placeholder');

    const [
        isUploading,
        setIsUploading,
    ] = useState(false);

    const [
        error,
        setError,
    ] = useState<string | null>(
        null,
    );

    const [
        preview,
        setPreview,
    ] = useState<string | null>(
        value || null,
    );

    const fileInputRef =
        useRef<HTMLInputElement>(
            null,
        );

    useEffect(() => {
        setPreview(
            value || null,
        );
    }, [value]);

    const getImageUrl = (
        url: string,
    ): string => {
        if (
            url.startsWith(
                'http://',
            ) ||
            url.startsWith(
                'https://',
            )
        ) {
            return url;
        }

        if (
            url.startsWith('/')
        ) {
            return url;
        }

        return `/${url}`;
    };

    const handleFileChange =
        async (
            event: React.ChangeEvent<HTMLInputElement>,
        ) => {
            const file =
                event.target.files?.[0];

            if (!file) {
                return;
            }

            setError(null);

            if (
                file.size >
                maxSize *
                1024 *
                1024
            ) {
                setError(
                    t(
                        'fileUpload.maxSize',
                        {
                            maxSize,
                        },
                    ),
                );

                event.target.value =
                    '';

                return;
            }

            if (
                !file.type.startsWith(
                    'image/',
                )
            ) {
                setError(
                    t(
                        'fileUpload.imageOnly',
                    ),
                );

                event.target.value =
                    '';

                return;
            }

            setIsUploading(
                true,
            );

            try {
                const formData =
                    new FormData();

                formData.append(
                    'file',
                    file,
                );

                const uploadUrl =
                    '/api/uploads';

                const response =
                    await fetch(
                        uploadUrl,
                        {
                            method:
                                'POST',
                            body:
                                formData,
                            credentials:
                                'include',
                        },
                    );

                if (
                    !response.ok
                ) {
                    const errorText =
                        await response.text();

                    console.error(
                        'Upload error response:',
                        errorText,
                    );

                    throw new Error(
                        t(
                            'fileUpload.uploadError',
                            {
                                status:
                                    response.status,
                            },
                        ),
                    );
                }

                const data =
                    await response.json();

                if (
                    !data ||
                    typeof data.url !==
                    'string' ||
                    !data.url
                ) {
                    throw new Error(
                        t(
                            'fileUpload.invalidResponse',
                        ),
                    );
                }

                const fileUrl =
                    data.url;

                setPreview(
                    fileUrl,
                );

                onChange(
                    fileUrl,
                );

                setError(
                    null,
                );
            } catch (
            err
            ) {
                console.error(
                    'Upload error:',
                    err,
                );

                setError(
                    err instanceof Error
                        ? err.message
                        : t(
                            'fileUpload.uploadFailed',
                        ),
                );
            } finally {
                setIsUploading(
                    false,
                );
            }
        };

    const handleRemove =
        () => {
            setPreview(
                null,
            );

            onChange(
                '',
            );

            onRemove?.();

            if (
                fileInputRef.current
            ) {
                fileInputRef.current.value =
                    '';
            }
        };

    const handleClick =
        () => {
            if (
                !isUploading
            ) {
                fileInputRef.current?.click();
            }
        };

    return (
        <div
            className={cn(
                'space-y-2',
                className,
            )}
        >
            <div
                className={cn(
                    'relative cursor-pointer rounded-lg border-2 border-dashed p-6',
                    'transition-colors duration-200',
                    'hover:border-primary/50',
                    error
                        ? 'border-red-500'
                        : 'border-gray-300',
                )}
                onClick={
                    handleClick
                }
            >
                <input
                    ref={
                        fileInputRef
                    }
                    type="file"
                    accept={
                        accept
                    }
                    onChange={
                        handleFileChange
                    }
                    className="hidden"
                    disabled={
                        isUploading
                    }
                />

                {isUploading ? (
                    <div className="flex flex-col items-center justify-center py-4">
                        <Loader2 className="h-10 w-10 animate-spin text-primary" />

                        <p className="mt-2 text-sm text-gray-500">
                            {t(
                                'fileUpload.uploading',
                            )}
                        </p>
                    </div>
                ) : preview ? (
                    <div className="relative">
                        <img
                            src={getImageUrl(
                                preview,
                            )}
                            alt="Preview"
                            className="mx-auto max-h-48 w-auto rounded-lg object-contain"
                            onError={event => {
                                console.error(
                                    'Image load error:',
                                    preview,
                                );

                                const target =
                                    event.currentTarget;

                                if (
                                    !target.dataset.fallback &&
                                    preview.startsWith(
                                        '/',
                                    )
                                ) {
                                    target.dataset.fallback =
                                        'true';

                                    target.src =
                                        `http://localhost:5000${preview}`;
                                }
                            }}
                        />

                        <button
                            type="button"
                            onClick={event => {
                                event.stopPropagation();

                                handleRemove();
                            }}
                            className="absolute -right-2 -top-2 rounded-full bg-red-500 p-1 text-white transition-colors hover:bg-red-600"
                            aria-label="Remove file"
                        >
                            <X className="h-4 w-4" />
                        </button>
                    </div>
                ) : (
                    <div className="flex flex-col items-center justify-center py-4 text-gray-500">
                        <Upload className="mb-2 h-10 w-10" />

                        <p className="text-sm">
                            {
                                uploadPlaceholder
                            }
                        </p>

                        <p className="mt-1 text-xs">
                            {accept ===
                                'image/*'
                                ? t(
                                    'fileUpload.images',
                                )
                                : accept}{' '}
                            •{' '}
                            {t(
                                'fileUpload.maxSizeHint',
                                {
                                    maxSize,
                                },
                            )}
                        </p>

                        <Button
                            type="button"
                            variant="outline"
                            size="sm"
                            className="mt-3"
                            onClick={event => {
                                event.stopPropagation();

                                handleClick();
                            }}
                        >
                            {t(
                                'fileUpload.chooseFile',
                            )}
                        </Button>
                    </div>
                )}
            </div>

            {error && (
                <p className="text-sm text-red-500">
                    {error}
                </p>
            )}
        </div>
    );
}