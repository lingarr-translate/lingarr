import { ILanguage } from '@/ts/language'

export const SETTINGS = {
    RADARR_API_KEY: 'radarr_api_key',
    RADARR_URL: 'radarr_url',
    SONARR_API_KEY: 'sonarr_api_key',
    SONARR_URL: 'sonarr_url',
    SOURCE_LANGUAGES: 'source_languages',
    TARGET_LANGUAGES: 'target_languages',
    SONARR_SETTINGS_COMPLETED: 'sonarr_settings_completed',
    RADARR_SETTINGS_COMPLETED: 'radarr_settings_completed',
    SERVICE_TYPE: 'service_type',
    LIBRETRANSLATE_URL: 'libretranslate_url',
    DEEPL_API_KEY: 'deepl_api_key',
    SHOW_SCHEDULE: 'show_schedule',
    MOVIE_SCHEDULE: 'movie_schedule',
    MAX_TRANSLATIONS_PER_RUN: 'max_translations_per_run',
    AUTOMATION_ENABLED: 'automation_enabled',
    TRANSLATION_SCHEDULE: 'translation_schedule'
} as const

type SettingKeys = (typeof SETTINGS)[keyof typeof SETTINGS]
type LanguageSettings = 'source_languages' | 'target_languages'

export type ISettings = {
    [K in SettingKeys]: K extends LanguageSettings ? string | ILanguage[] : string
}
