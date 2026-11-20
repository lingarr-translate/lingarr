import { ILanguage } from '@/ts/language'
import { ILocale, ITheme } from '@/ts/store'

export const SETTINGS = {
    RADARR_URL: 'radarr_url',
    SONARR_URL: 'sonarr_url',
    SOURCE_LANGUAGES: 'source_languages',
    TARGET_LANGUAGES: 'target_languages',
    SONARR_SETTINGS_COMPLETED: 'sonarr_settings_completed',
    RADARR_SETTINGS_COMPLETED: 'radarr_settings_completed',
    SHOW_SCHEDULE: 'show_schedule',
    MOVIE_SCHEDULE: 'movie_schedule',
    MAX_TRANSLATIONS_PER_RUN: 'max_translations_per_run',
    AUTOMATION_ENABLED: 'automation_enabled',
    TRANSLATION_SCHEDULE: 'translation_schedule',
    AI_PROMPT: 'ai_prompt',
    AI_USER_PROMPT: 'ai_user_prompt',
    PROOFREAD_PROMPT: 'proofread_prompt',
    PROOFREAD_USER_PROMPT: 'proofread_user_prompt',
    THEME: 'theme',
    LOCALE: 'locale',
    MOVIE_AGE_THRESHOLD: 'movie_age_threshold',
    SHOW_AGE_THRESHOLD: 'show_age_threshold',
    FIX_OVERLAPPING_SUBTITLES: 'fix_overlapping_subtitles',
    STRIP_SUBTITLE_FORMATTING: 'strip_subtitle_formatting',
    PRESERVE_LINE_BREAKS: 'preserve_line_breaks',
    ADD_TRANSLATOR_INFO: 'add_translator_info',
    SUBTITLE_VALIDATION_ENABLED: 'subtitle_validation_enabled',
    SUBTITLE_VALIDATION_MAXDURATIONSECS: 'subtitle_validation_maxdurationsecs',
    SUBTITLE_VALIDATION_MINDURATIONMS: 'subtitle_validation_mindurationms',
    SUBTITLE_VALIDATION_MINSUBTITLELENGTH: 'subtitle_validation_minsubtitlelength',
    SUBTITLE_VALIDATION_MAXSUBTITLELENGTH: 'subtitle_validation_maxsubtitlelength',
    SUBTITLE_VALIDATION_MAXFILESIZEBYTES: 'subtitle_validation_maxfilesizebytes',
    AI_CONTEXT_BEFORE: 'ai_context_before',
    AI_CONTEXT_AFTER: 'ai_context_after',
    AI_CONTEXT_USE_TRANSLATED: 'ai_context_use_translated',
    USE_BATCH_TRANSLATION: 'use_batch_translation',
    MAX_BATCH_SIZE: 'max_batch_size',
    USE_SUBTITLE_TAGGING: 'use_subtitle_tagging',
    REMOVE_LANGUAGE_TAG: 'remove_language_tag',
    SUBTITLE_TAG: 'subtitle_tag',
    IGNORE_CAPTIONS: 'ignore_captions',
    REQUEST_TIMEOUT: 'request_timeout',
    MAX_RETRIES: 'max_retries',
    RETRY_DELAY: 'retry_delay',
    RETRY_DELAY_MULTIPLIER: 'retry_delay_multiplier',
    AUTH_ENABLED: 'auth_enabled',
    ONBOARDING_COMPLETED: 'onboarding_completed',
    TELEMETRY_ENABLED: 'telemetry_enabled',
    NAVIGATE_TO_DETAILS_ON_REQUEST: 'navigate_to_details_on_request',
    LANGUAGE_CODE_FORMAT: 'language_code_format',
    RADARR_DEFAULT_INCLUDE: 'radarr_default_include',
    SONARR_DEFAULT_INCLUDE: 'sonarr_default_include',
    RADARR_EXCLUDE_UNMONITORED: 'radarr_exclude_unmonitored',
    SONARR_EXCLUDE_UNMONITORED_SERIES: 'sonarr_exclude_unmonitored_series',
    SONARR_EXCLUDE_UNMONITORED_EPISODES: 'sonarr_exclude_unmonitored_episodes'
} as const

export interface ISettings {
    radarr_url: string
    radarr_exclude_unmonitored: string
    sonarr_exclude_unmonitored_series: string
    sonarr_exclude_unmonitored_episodes: string
    sonarr_url: string
    show_schedule: string
    movie_schedule: string
    max_translations_per_run: string
    translation_schedule: string
    source_languages: string | ILanguage[]
    target_languages: string | ILanguage[]
    automation_enabled: string
    sonarr_settings_completed: string
    radarr_settings_completed: string
    ai_prompt: string
    ai_user_prompt: string
    proofread_prompt: string
    proofread_user_prompt: string
    movie_age_threshold: string
    show_age_threshold: string
    fix_overlapping_subtitles: string
    strip_subtitle_formatting: string
    preserve_line_breaks: string
    add_translator_info: string
    theme: ITheme
    locale: ILocale
    subtitle_validation_enabled: string
    subtitle_validation_maxfilesizebytes: string
    subtitle_validation_minsubtitlelength: string
    subtitle_validation_maxsubtitlelength: string
    subtitle_validation_mindurationms: string
    subtitle_validation_maxdurationsecs: string
    ai_context_before: string
    ai_context_after: string
    ai_context_use_translated: string
    use_batch_translation: string
    max_batch_size: string
    use_subtitle_tagging: string
    remove_language_tag: string
    subtitle_tag: string
    ignore_captions: string
    request_timeout: string
    max_retries: string
    retry_delay: string
    retry_delay_multiplier: string
    auth_enabled: string
    onboarding_completed: string
    telemetry_enabled: string
    navigate_to_details_on_request: string
    language_code_format: string
    radarr_default_include: string
    sonarr_default_include: string
}


export const ENCRYPTED_SETTINGS = {
    API_KEY: 'api_key',
    RADARR_API_KEY: 'radarr_api_key',
    SONARR_API_KEY: 'sonarr_api_key'
} as const

export interface IEncryptedSettings {
    api_key: string
    radarr_api_key: string
    sonarr_api_key: string
}

export const SERVICE_TYPE = {
    LIBRETRANSLATE: 'libretranslate',
    OPENAI: 'openai',
    ANTHROPIC: 'anthropic',
    LOCALAI: 'localai',
    DEEPL: 'deepl',
    GEMINI: 'gemini',
    DEEPSEEK: 'deepseek',
    MISTRAL: 'mistral',
    XAI: 'xai',
    GOOGLE: 'google',
    BING: 'bing',
    MICROSOFT: 'microsoft',
    YANDEX: 'yandex'
} as const

export type ServiceType = (typeof SERVICE_TYPE)[keyof typeof SERVICE_TYPE]

export const BATCH_SERVICE_TYPES: string[] = [
    SERVICE_TYPE.ANTHROPIC,
    SERVICE_TYPE.GEMINI,
    SERVICE_TYPE.LOCALAI,
    SERVICE_TYPE.MISTRAL,
    SERVICE_TYPE.OPENAI,
    SERVICE_TYPE.XAI
]

export const REQUEST_TEMPLATE_KEYS: Record<string, { key: string; generateKey?: string }> = {
    [SERVICE_TYPE.OPENAI]: { key: 'openai_request_template' },
    [SERVICE_TYPE.ANTHROPIC]: { key: 'anthropic_request_template' },
    [SERVICE_TYPE.LOCALAI]: {
        key: 'local_ai_chat_request_template',
        generateKey: 'local_ai_generate_request_template'
    },
    [SERVICE_TYPE.GEMINI]: { key: 'gemini_request_template' },
    [SERVICE_TYPE.DEEPSEEK]: { key: 'deepseek_request_template' },
    [SERVICE_TYPE.MISTRAL]: { key: 'mistral_request_template' },
    [SERVICE_TYPE.XAI]: { key: 'xai_request_template' }
}

export interface IFilterOptions {
    logLevel: string
}

export interface ILogEntry {
    logLevel: string
    message: string
    timestamp: string
    formattedSource: string
    category: string
    stackTrace?: string
}
