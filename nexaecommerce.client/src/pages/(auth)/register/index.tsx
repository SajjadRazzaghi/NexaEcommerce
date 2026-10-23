import { useMemo, useState, type ReactNode } from 'react';
import { useTranslation } from 'react-i18next';
import { Link, useNavigate, useSearchParams } from 'react-router';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { useMutation } from '@tanstack/react-query';
import { z } from 'zod';
import {
    CircleCheck,
    Loader2,
    Lock,
    MailCheck,
} from 'lucide-react';

import { authApi } from '@/lib/api/auth';
import { mergeGuestCart } from '@/modules/cart/api/cartApi';
import { applyApiErrorToForm, type FormBannerState } from '@/lib/api/form-errors';
import { useDocumentTitle } from '@/hooks/use-document-title';
import { useSetCurrentUser } from '@/hooks/use-auth';
import { usePublicConfig } from '@/hooks/use-public-config';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Form, FormControl, FormDescription, FormField, FormItem, FormLabel, FormMessage } from '@/components/ui/form';
import { PasswordInput } from '@/components/auth/password-input';
import { FormBanner } from '@/components/auth/form-banner';
import { meta } from './meta';

type Values = {
    displayName?: string;
    identifier: string;
    password: string;
    confirmPassword: string;
};

export default function RegisterPage() {
  const { t } = useTranslation();
  useDocumentTitle(meta.title);
  const [banner, setBanner] = useState<FormBannerState | null>(null);
  const { data: publicConfig } = usePublicConfig();
    const navigate = useNavigate();
    const [params] = useSearchParams();
    const returnUrl = safeReturn(params.get('returnUrl'));
    const setCurrentUser = useSetCurrentUser();
    const schema = useMemo(
        () =>
            z
                .object({
                    displayName: z.string().max(100).optional(),

                    identifier: z
                        .string()
                        .trim()
                        .min(
                            1,
                            t('auth.valid.identifierRequired'),
                        ),

                    password: z
                        .string()
                        .min(6, t('auth.valid.passwordMin'))
                        .max(128),

                    confirmPassword: z
                        .string()
                        .min(1, t('auth.valid.confirmRequired')),
                })
                .refine(
                    (v) => v.password === v.confirmPassword,
                    {
                        message: t('auth.valid.mismatch'),
                        path: ['confirmPassword'],
                    },
                ),
        [t],
    );

  const form = useForm<Values>({
    resolver: zodResolver(schema),
      defaultValues: {
          displayName: '',
          identifier: '',
          password: '',
          confirmPassword: '',
      },
  });
    const register = useMutation({
        mutationFn: async (values: {
            identifier: string;
            password: string;
            displayName?: string;
        }) => {
            const registration = await authApi.register(values);

            // اگر تأیید ایمیل لازم است، ابتدا همان روند قبلی اجرا شود.
            if (registration.requiresEmailConfirmation) {
                return {
                    registration,
                    loginResult: null,
                };
            }

            // ورود خودکار پس از ثبت‌نام با ایمیل یا شماره موبایل
            const loginResult = await authApi.login({
                identifier: values.identifier,
                password: values.password,
                rememberMe: true,
            });

            return {
                registration,
                loginResult,
            };
        },

        onSuccess: async ({ registration, loginResult }) => {
            // در حالت نیاز به تأیید ایمیل، کاربر فعلاً وارد نمی‌شود.
            if (
                registration.requiresEmailConfirmation ||
                !loginResult?.user
            ) {
                return;
            }

            setCurrentUser(loginResult.user);

            // ادغام سبد خرید مهمان؛ خطای آن نباید ورود موفق را لغو کند.
            try {
                await mergeGuestCart();
            } catch {
                // Guest-cart merge is best-effort.
            }

            navigate(returnUrl, {
                replace: true,
            });
        },

        onError: (error) =>
            setBanner(
                applyApiErrorToForm(
                    error,
                    form.setError,
                    ['identifier', 'password'],
                ),
            ),
    });
    function safeReturn(value: string | null): string {
        return value &&
            value.startsWith('/') &&
            !value.startsWith('//')
            ? value
            : '/';
    }
    const onSubmit = form.handleSubmit((values) => {
        setBanner(null);

        register.mutate({
            identifier: values.identifier,
            password: values.password,
            displayName: values.displayName || undefined,
        });
    });

  if (publicConfig && !publicConfig.allowRegistration) {
    return (
      <div className="grid gap-4 text-center">
        <div className="bg-muted text-muted-foreground mx-auto grid size-12 place-items-center rounded-full">
          <Lock className="size-6" />
        </div>
        <h1 className="text-2xl font-semibold tracking-tight">{t('auth.register.closedTitle')}</h1>
        <p className="text-muted-foreground text-sm text-balance">{t('auth.register.closedDesc')}</p>
        <Button asChild variant="outline" className="mt-2">
          <Link to="/login">{t('auth.backToSignIn')}</Link>
        </Button>
      </div>
    );
  }

    if (register.isSuccess) {
        const requiresEmailConfirmation =
            register.data?.registration.requiresEmailConfirmation ?? false;

        return (
            <div className="grid gap-4 text-center">
                <div className="bg-success/10 text-success mx-auto grid size-12 place-items-center rounded-full">
                    {requiresEmailConfirmation ? (
                        <MailCheck className="size-6" />
                    ) : (
                        <CircleCheck className="size-6" />
                    )}
                </div>

                <h1 className="text-2xl font-semibold tracking-tight">
                    {requiresEmailConfirmation
                        ? t(
                            'auth.register.checkEmailTitle',
                            'Check your email',
                        )
                        : t(
                            'auth.register.successTitle',
                            'Registration successful',
                        )}
                </h1>

                <p className="text-muted-foreground text-sm text-balance">
                    {requiresEmailConfirmation
                        ? t(
                            'auth.register.checkEmailDesc',
                            {
                                email: form.getValues('identifier'),
                            },
                        )
                        : t(
                            'auth.register.successDesc',
                            'Your account has been created successfully. You can now sign in.',
                        )}
                </p>

                <Button asChild className="mt-2">
                    <Link to="/login">
                        {t('auth.backToSignIn')}
                    </Link>
                </Button>
            </div>
        );
    }

  // Optional OAuth sign-in buttons — built in TS into a const slot so the JSX below needs no build-time conditional.
  const slots: Record<string, ReactNode> = {};

  return (
    <div className="grid gap-6">
      <header className="grid gap-1.5">
        <h1 className="text-2xl font-semibold tracking-tight">{t('auth.register.title')}</h1>
        <p className="text-muted-foreground text-sm">{t('auth.register.subtitle')}</p>
      </header>

      <FormBanner state={banner} />

      <Form {...form}>
        <form onSubmit={onSubmit} className="grid gap-4" noValidate>
          <FormField
            control={form.control}
            name="displayName"
            render={({ field }) => (
              <FormItem>
                <FormLabel>{t('auth.register.name')}</FormLabel>
                <FormControl>
                  <Input autoComplete="name" placeholder={t('auth.register.namePlaceholder')} {...field} />
                </FormControl>
                <FormMessage />
              </FormItem>
            )}
          />

                  <FormField
                      control={form.control}
                      name="identifier"
                      render={({ field }) => (
                          <FormItem>
                              <FormLabel>
                                  {t('auth.identifier')}
                              </FormLabel>

                              <FormControl>
                                  <Input
                                      type="text"
                                      autoComplete="username"
                                      placeholder={t('auth.identifierPlaceholder')}
                                      {...field}
                                  />
                              </FormControl>

                              <FormMessage />
                          </FormItem>
                      )}
                  />
          <FormField
            control={form.control}
            name="password"
            render={({ field }) => (
              <FormItem>
                <FormLabel>{t('auth.password')}</FormLabel>
                <FormControl>
                  <PasswordInput autoComplete="new-password" placeholder={t('auth.passwordPlaceholder')} {...field} />
                </FormControl>
                <FormDescription>{t('auth.register.passwordHint')}</FormDescription>
                <FormMessage />
              </FormItem>
            )}
          />

          <FormField
            control={form.control}
            name="confirmPassword"
            render={({ field }) => (
              <FormItem>
                <FormLabel>{t('auth.register.confirm')}</FormLabel>
                <FormControl>
                  <PasswordInput autoComplete="new-password" placeholder={t('auth.passwordPlaceholder')} {...field} />
                </FormControl>
                <FormMessage />
              </FormItem>
            )}
          />

          <Button type="submit" disabled={register.isPending}>
            {register.isPending && <Loader2 className="animate-spin" />}
            {t('auth.register.submit')}
          </Button>
        </form>
      </Form>

      {slots.oauthButtons}

      <p className="text-muted-foreground text-center text-sm">
        {t('auth.register.haveAccount')}{' '}
        <Link to="/login" className="text-foreground font-medium hover:underline">
          {t('auth.register.signIn')}
        </Link>
      </p>
    </div>
  );
}

