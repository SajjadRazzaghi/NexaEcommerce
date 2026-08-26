import { jsx as _jsx, jsxs as _jsxs } from "react/jsx-runtime";
import { useTranslation } from 'react-i18next';
import { useDocumentTitle } from '@/hooks/use-document-title';
import { SectionLayout } from '@/components/section-layout';
import { ProfileInfoSection } from '@/components/profile/profile-info-section';
import { PreferencesSection } from '@/components/profile/preferences-section';
import { PasswordSection } from '@/components/profile/password-section';
import { meta } from './meta';
export default function ProfilePage() {
    const { t } = useTranslation();
    useDocumentTitle(meta.title);
    return (_jsxs("div", { className: "grid gap-6", children: [_jsxs("header", { children: [_jsx("h1", { className: "text-2xl font-semibold tracking-tight", children: t('profile.title') }), _jsx("p", { className: "text-muted-foreground mt-1", children: t('profile.subtitle') })] }), _jsx(SectionLayout, { side: "end", sections: [
                    { id: 'profile', label: t('profile.sections.profile'), content: _jsx(ProfileInfoSection, {}) },
                    { id: 'preferences', label: t('profile.sections.preferences'), content: _jsx(PreferencesSection, {}) },
                    { id: 'password', label: t('profile.sections.password'), content: _jsx(PasswordSection, {}) },
                ] })] }));
}
