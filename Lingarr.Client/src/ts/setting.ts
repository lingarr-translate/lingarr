import { ILanguage } from '@/ts/language'
import { ILocale, ITheme } from '@/ts/store'

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
    TRANSLATION_SCHEDULE: 'translation_schedule',
    OPENAI_MODEL: 'openai_model',
    OPENAI_API_KEY: 'openai_api_key',
    ANTHROPIC_MODEL: 'anthropic_model',
    ANTHROPIC_API_KEY: 'anthropic_api_key',
    ANTHROPIC_VERSION: 'anthropic_version',
    LOCAL_AI_ENDPOINT: 'local_ai_endpoint',
    LOCAL_AI_MODEL: 'local_ai_model',
    LOCAL_AI_API_KEY: 'local_ai_api_key',
    GEMINI_MODEL: 'gemini_model',
    GEMINI_API_KEY: 'gemini_api_key',
    DEEPSEEK_MODEL: 'deepseek_model',
    DEEPSEEK_API_KEY: 'deepseek_api_key',
    AI_PROMPT: 'ai_prompt',
    THEME: 'theme',
    LOCALE: 'locale',
    MOVIE_AGE_THRESHOLD: 'movie_age_threshold',
    SHOW_AGE_THRESHOLD: 'show_age_threshold',
    FIX_OVERLAPPING_SUBTITLES: 'fix_overlapping_subtitles',
    STRIP_SUBTITLE_FORMATTING: 'strip_subtitle_formatting',
    CUSTOM_AI_PARAMETERS: 'custom_ai_parameters',
    SUBTITLE_VALIDATION_ENABLED: 'subtitle_validation_enabled',
    SUBTITLE_VALIDATION_MAXDURATIONSECS: 'subtitle_validation_maxdurationsecs',
    SUBTITLE_VALIDATION_MINDURATIONMS: 'subtitle_validation_mindurationms',
    SUBTITLE_VALIDATION_MINSUBTITLELENGTH: 'subtitle_validation_minsubtitlelength',
    SUBTITLE_VALIDATION_MAXSUBTITLELENGTH: 'subtitle_validation_maxsubtitlelength',
    SUBTITLE_VALIDATION_MAXFILESIZEBYTES: 'subtitle_validation_maxfilesizebytes'
} as const

export interface ISettings {
    radarr_api_key: string
    radarr_url: string
    sonarr_api_key: string
    sonarr_url: string
    service_type: string
    libretranslate_url: string
    deepl_api_key: string
    show_schedule: string
    movie_schedule: string
    max_translations_per_run: string
    translation_schedule: string
    source_languages: string | ILanguage[]
    target_languages: string | ILanguage[]
    automation_enabled: string
    sonarr_settings_completed: string
    radarr_settings_completed: string
    openai_model: string
    openai_api_key: string
    anthropic_model: string
    anthropic_api_key: string
    anthropic_version: string
    local_ai_endpoint: string
    local_ai_model: string
    local_ai_api_key: string
    gemini_model: string
    gemini_api_key: string
    deepseek_model: string
    deepseek_api_key: string
    ai_prompt: string
    movie_age_threshold: string
    show_age_threshold: string
    fix_overlapping_subtitles: string
    strip_subtitle_formatting: string
    theme: ITheme
    locale: ILocale
    custom_ai_parameters: string | ICustomAiParams[]
    subtitle_validation_enabled: string
    subtitle_validation_maxfilesizebytes: string
    subtitle_validation_minsubtitlelength: string
    subtitle_validation_maxsubtitlelength: string
    subtitle_validation_mindurationms: string
    subtitle_validation_maxdurationsecs: string
}

export interface ICustomAiParams {
    key: string
    value: string
}

export const SERVICE_TYPE = {
    LIBRETRANSLATE: 'libretranslate',
    OPENAI: 'openai',
    ANTHROPIC: 'anthropic',
    LOCALAI: 'localai',
    DEEPL: 'deepl',
    GEMINI: 'gemini',
    DEEPSEEK: 'deepseek',
    GOOGLE: 'google',
    BING: 'bing',
    MICROSOFT: 'microsoft',
    YANDEX: 'yandex'
} as const

export type ServiceType = (typeof SERVICE_TYPE)[keyof typeof SERVICE_TYPE]
