import { useTranslation } from 'react-i18next';
import './LanguageSelector.css';

export function LanguageSelector() {
    const { i18n, t } = useTranslation();

    const changeLanguage = (lng: string) => {
        i18n.changeLanguage(lng);
    };

    return (
        <div className="language-selector">
            <span className="language-selector__label">{t('common.language')}:</span>
            <select
                className="language-selector__select"
                value={i18n.language}
                onChange={(e) => changeLanguage(e.target.value)}
                aria-label={t('common.language')}
            >
                <option value="en">EN</option>
                <option value="vi">VI</option>
            </select>
        </div>
    );
}
