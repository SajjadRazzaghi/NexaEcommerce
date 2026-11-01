import { useEffect, useState, type FormEvent } from 'react';
import { AlertCircle, Bot, Database, LoaderCircle, Send, ShieldCheck, Sparkles } from 'lucide-react';
import { useTranslation } from 'react-i18next';

import { usePermission } from '@/hooks/use-permission';
import { api } from '@/lib/api/client';

type AssistantInfo = {
    mode: string;
    providerName: string;
    externalProviderConnected: boolean;
    dataSentExternally: boolean;
    dataCategories: string[];
    processingLocation: string | null;
    externalApiCost: string;
    developmentCostSeparate: boolean;
    developmentCostNote: string;
    writtenApprovalRequiredBeforeExternalUse: boolean;
    writeActionsEnabled: boolean;
    businessTimeZoneId: string;
};

type AssistantDetail = {
    label: string;
    value: string;
};

type AssistantResponse = {
    answer: string;
    dataStatus: 'live-data' | 'system-guide' | 'insufficient-data' | 'permission-denied' | string;
    source: string;
    details: AssistantDetail[];
    dataAsOfUtc: string | null;
    externalDataSent: boolean;
};

const EXAMPLE_QUESTION_KEYS = [
    'assistant.exampleUnpaidOrders',
    'assistant.exampleLowStock',
    'assistant.exampleSales',
] as const;

export default function AssistantPage() {
    const { t } = useTranslation();
    const canUseAssistant = usePermission('assistant.use');
    const [info, setInfo] = useState<AssistantInfo | null>(null);
    const [infoError, setInfoError] = useState(false);
    const [question, setQuestion] = useState('');
    const [answer, setAnswer] = useState<AssistantResponse | null>(null);
    const [error, setError] = useState('');
    const [isLoading, setIsLoading] = useState(false);

    useEffect(() => {
        if (!canUseAssistant) return;

        let cancelled = false;
        api.get<AssistantInfo>('/assistant/info')
            .then((value) => {
                if (!cancelled) setInfo(value);
            })
            .catch(() => {
                if (!cancelled) setInfoError(true);
            });

        return () => {
            cancelled = true;
        };
    }, [canUseAssistant]);

    const submitQuestion = async (event: FormEvent<HTMLFormElement>) => {
        event.preventDefault();
        const value = question.trim();
        if (!value || isLoading) return;

        setIsLoading(true);
        setError('');
        setAnswer(null);
        try {
            const response = await api.post<AssistantResponse>('/assistant/query', { question: value });
            setAnswer(response);
        } catch {
            setError(t('assistant.queryError'));
        } finally {
            setIsLoading(false);
        }
    };

    if (!canUseAssistant) {
        return (
            <section className="mx-auto max-w-3xl space-y-4" dir="auto">
                <div className="flex items-center gap-3">
                    <div className="rounded-xl border bg-muted p-3"><Bot className="size-6" /></div>
                    <div>
                        <h1 className="text-2xl font-semibold">{t('assistant.title')}</h1>
                        <p className="text-sm text-muted-foreground">{t('assistant.noPermission')}</p>
                    </div>
                </div>
            </section>
        );
    }

    const statusLabel = (status: AssistantResponse['dataStatus']) => {
        if (status === 'live-data') return t('assistant.liveData');
        if (status === 'system-guide') return t('assistant.systemGuide');
        if (status === 'permission-denied') return t('assistant.permissionDenied');
        if (status === 'action-disabled') return t('assistant.actionDisabled');
        return t('assistant.insufficientData');
    };

    return (
        <section className="mx-auto max-w-4xl space-y-6" dir="auto">
            <header className="flex items-start gap-3">
                <div className="rounded-xl border bg-muted p-3">
                    <Bot className="size-7" aria-hidden="true" />
                </div>
                <div className="min-w-0 flex-1">
                    <h1 className="text-2xl font-semibold tracking-tight">{t('assistant.title')}</h1>
                    <p className="mt-1 text-sm text-muted-foreground">{t('assistant.description')}</p>
                </div>
            </header>

            <div className="rounded-xl border border-emerald-700/20 bg-emerald-500/5 p-4">
                <div className="flex items-start gap-3">
                    <ShieldCheck className="mt-0.5 size-5 shrink-0 text-emerald-700" aria-hidden="true" />
                    <div className="min-w-0 space-y-1">
                        <h2 className="font-semibold">{t('assistant.localOnlyTitle')}</h2>
                        <p className="text-sm text-muted-foreground">{t('assistant.localOnlyDescription')}</p>
                        <p className="text-sm font-medium">{t('assistant.readOnlyNotice')}</p>
                    </div>
                </div>
            </div>

            <div className="grid gap-3 sm:grid-cols-2">
                <div className="rounded-xl border p-4">
                    <div className="mb-2 flex items-center gap-2 text-sm font-semibold">
                        <Database className="size-4" aria-hidden="true" />
                        {t('assistant.dataDisclosureTitle')}
                    </div>
                    <dl className="space-y-3 text-sm">
                        <div>
                            <dt className="text-muted-foreground">{t('assistant.providerLabel')}</dt>
                            <dd className="mt-1 font-medium">{info?.externalProviderConnected ? info.providerName : t('assistant.localProvider')}</dd>
                        </div>
                        <div>
                            <dt className="text-muted-foreground">{t('assistant.dataLabel')}</dt>
                            <dd className="mt-1">{t('assistant.dataCategories')}</dd>
                        </div>
                        <div>
                            <dt className="text-muted-foreground">{t('assistant.locationLabel')}</dt>
                            <dd className="mt-1">{info?.processingLocation || t('assistant.locationUnknown')}</dd>
                        </div>
                        <div>
                            <dt className="text-muted-foreground">{t('assistant.costLabel')}</dt>
                            <dd className="mt-1 font-medium">{info?.externalProviderConnected ? info.externalApiCost : t('assistant.noExternalCost')}</dd>
                        </div>
                        <div>
                            <dt className="text-muted-foreground">{t('assistant.developmentCostLabel')}</dt>
                            <dd className="mt-1">{t('assistant.developmentCostSeparate')}</dd>
                        </div>
                        <div>
                            <dt className="text-muted-foreground">{t('assistant.timezoneLabel')}</dt>
                            <dd className="mt-1">{info?.businessTimeZoneId ?? 'Iran Standard Time'}</dd>
                        </div>
                    </dl>
                    {(infoError || !info) && (
                        <p className="mt-3 text-xs text-muted-foreground">
                            {infoError ? t('assistant.infoLoadError') : t('assistant.infoLoading')}
                        </p>
                    )}
                </div>

                <div className="rounded-xl border p-4">
                    <div className="mb-2 flex items-center gap-2 text-sm font-semibold">
                        <Sparkles className="size-4" aria-hidden="true" />
                        {t('assistant.examplesTitle')}
                    </div>
                    <div className="space-y-2">
                        {EXAMPLE_QUESTION_KEYS.map((exampleKey) => (
                            <button
                                key={exampleKey}
                                type="button"
                                onClick={() => setQuestion(t(exampleKey))}
                                className="w-full rounded-lg border px-3 py-2 text-start text-sm transition hover:bg-muted focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring"
                            >
                                {t(exampleKey)}
                            </button>
                        ))}
                    </div>
                    <p className="mt-3 text-xs text-muted-foreground">{t('assistant.approvalNotice')}</p>
                </div>
            </div>

            <form onSubmit={submitQuestion} className="space-y-3 rounded-xl border p-4 sm:p-5">
                <label htmlFor="assistant-question" className="block text-sm font-medium">
                    {t('assistant.questionLabel')}
                </label>
                <textarea
                    id="assistant-question"
                    value={question}
                    onChange={(event) => setQuestion(event.target.value)}
                    maxLength={1000}
                    rows={3}
                    placeholder={t('assistant.questionPlaceholder')}
                    className="w-full resize-y rounded-lg border bg-background px-3 py-2 text-sm shadow-sm outline-none placeholder:text-muted-foreground focus-visible:ring-2 focus-visible:ring-ring"
                    required
                />
                <div className="flex flex-wrap items-center justify-between gap-3">
                    <p className="text-xs text-muted-foreground">{t('assistant.questionLimit', { count: question.length })}</p>
                    <button
                        type="submit"
                        disabled={!question.trim() || isLoading}
                        className="inline-flex items-center justify-center gap-2 rounded-lg bg-primary px-4 py-2 text-sm font-medium text-primary-foreground transition hover:opacity-90 disabled:cursor-not-allowed disabled:opacity-50"
                    >
                        {isLoading ? <LoaderCircle className="size-4 animate-spin" aria-hidden="true" /> : <Send className="size-4" aria-hidden="true" />}
                        {isLoading ? t('assistant.sending') : t('assistant.ask')}
                    </button>
                </div>
            </form>

            {error && (
                <div role="alert" className="flex gap-2 rounded-xl border border-destructive/30 bg-destructive/5 p-4 text-sm">
                    <AlertCircle className="mt-0.5 size-4 shrink-0 text-destructive" aria-hidden="true" />
                    <p>{error}</p>
                </div>
            )}

            {answer && (
                <article aria-live="polite" className="space-y-4 rounded-xl border p-4 sm:p-5">
                    <div className="flex flex-wrap items-center justify-between gap-2">
                        <h2 className="font-semibold">{t('assistant.answerTitle')}</h2>
                        <span className="rounded-full border px-2.5 py-1 text-xs text-muted-foreground">{statusLabel(answer.dataStatus)}</span>
                    </div>
                    <p className="whitespace-pre-wrap leading-7">{answer.answer}</p>
                    {answer.details.length > 0 && (
                        <div className="space-y-2">
                            <h3 className="text-sm font-semibold">{t('assistant.detailsTitle')}</h3>
                            <dl className="divide-y rounded-lg border">
                                {answer.details.map((item, index) => (
                                    <div key={`${item.label}-${index}`} className="grid gap-1 px-3 py-2.5 sm:grid-cols-[minmax(0,1fr)_minmax(0,1.5fr)] sm:gap-4">
                                        <dt className="text-sm text-muted-foreground">{item.label}</dt>
                                        <dd className="break-words text-sm font-medium">{item.value}</dd>
                                    </div>
                                ))}
                            </dl>
                        </div>
                    )}
                    <footer className="border-t pt-3 text-xs text-muted-foreground">
                        {answer.dataAsOfUtc
                            ? `${t('assistant.dataAsOf')}: ${new Date(answer.dataAsOfUtc).toLocaleString()}`
                            : answer.dataStatus === 'system-guide'
                                ? t('assistant.noLiveDataUsed')
                                : answer.dataStatus === 'action-disabled'
                                    ? t('assistant.noChangeMade')
                                    : null}
                        {' · '}{t('assistant.externalDataSent')}: {answer.externalDataSent ? t('assistant.yes') : t('assistant.no')}
                    </footer>
                </article>
            )}
        </section>
    );
}
