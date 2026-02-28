import { ILanguage } from '@/ts/language'
import { ILocale, ITheme } from '@/ts/store'

export const SETTINGS = {
    RADARR_URL: 'radarr_url',
    SONARR_URL: 'sonarr_url',
    SOURCE_LANGUAGES: 'source_languages',
    TARGET_LANGUAGES: 'target_languages',
    SONARR_SETTINGS_COMPLETED: 'sonarr_settings_completed',
    RADARR_SETTINGS_COMPLETED: 'radarr_settings_completed',
    SERVICE_TYPE: 'service_type',
    LIBRETRANSLATE_URL: 'libretranslate_url',
    SHOW_SCHEDULE: 'show_schedule',
    MOVIE_SCHEDULE: 'movie_schedule',
    MAX_TRANSLATIONS_PER_RUN: 'max_translations_per_run',
    AUTOMATION_ENABLED: 'automation_enabled',
    TRANSLATION_SCHEDULE: 'translation_schedule',
    OPENAI_MODEL: 'openai_model',
    ANTHROPIC_MODEL: 'anthropic_model',
    ANTHROPIC_VERSION: 'anthropic_version',
    LOCAL_AI_ENDPOINT: 'local_ai_endpoint',
    LOCAL_AI_MODEL: 'local_ai_model',
    GEMINI_MODEL: 'gemini_model',
    DEEPSEEK_MODEL: 'deepseek_model',
    AI_PROMPT: 'ai_prompt',
    THEME: 'theme',
    LOCALE: 'locale',
    MOVIE_AGE_THRESHOLD: 'movie_age_threshold',
    SHOW_AGE_THRESHOLD: 'show_age_threshold',
    FIX_OVERLAPPING_SUBTITLES: 'fix_overlapping_subtitles',
    STRIP_SUBTITLE_FORMATTING: 'strip_subtitle_formatting',
    ADD_TRANSLATOR_INFO: 'add_translator_info',
    CUSTOM_AI_PARAMETERS: 'custom_ai_parameters',
    SUBTITLE_VALIDATION_ENABLED: 'subtitle_validation_enabled',
    SUBTITLE_VALIDATION_MAXDURATIONSECS: 'subtitle_validation_maxdurationsecs',
    SUBTITLE_VALIDATION_MINDURATIONMS: 'subtitle_validation_mindurationms',
    SUBTITLE_VALIDATION_MINSUBTITLELENGTH: 'subtitle_validation_minsubtitlelength',
    SUBTITLE_VALIDATION_MAXSUBTITLELENGTH: 'subtitle_validation_maxsubtitlelength',
    SUBTITLE_VALIDATION_MAXFILESIZEBYTES: 'subtitle_validation_maxfilesizebytes',
    AI_CONTEXT_PROMPT_ENABLED: 'ai_context_prompt_enabled',
    AI_CONTEXT_PROMPT: 'ai_context_prompt',
    AI_CONTEXT_BEFORE: 'ai_context_before',
    AI_CONTEXT_AFTER: 'ai_context_after',
    USE_BATCH_TRANSLATION: 'use_batch_translation',
    MAX_BATCH_SIZE: 'max_batch_size',
    USE_SUBTITLE_TAGGING: 'use_subtitle_tagging',
    REMOVE_LANGUAGE_TAG: 'remove_language_tag',
    SUBTITLE_TAG: 'subtitle_tag',
    IGNORE_CAPTIONS: 'ignore_captions',
    MAX_RETRIES: 'max_retries',
    RETRY_DELAY: 'retry_delay',
    RETRY_DELAY_MULTIPLIER: 'retry_delay_multiplier',
    AUTH_ENABLED: 'auth_enabled',
    ONBOARDING_COMPLETED: 'onboarding_completed',
    TELEMETRY_ENABLED: 'telemetry_enabled',
    NAVIGATE_TO_DETAILS_ON_REQUEST: 'navigate_to_details_on_request',
    OPENAI_REQUEST_TEMPLATE: 'openai_request_template',
    ANTHROPIC_REQUEST_TEMPLATE: 'anthropic_request_template',
    LOCAL_AI_CHAT_REQUEST_TEMPLATE: 'local_ai_chat_request_template',
    LOCAL_AI_GENERATE_REQUEST_TEMPLATE: 'local_ai_generate_request_template',
    DEEPSEEK_REQUEST_TEMPLATE: 'deepseek_request_template',
    GEMINI_REQUEST_TEMPLATE: 'gemini_request_template'
} as const

export interface ISettings {
    radarr_url: string
    sonarr_url: string
    service_type: string
    libretranslate_url: string
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
    anthropic_model: string
    anthropic_version: string
    local_ai_endpoint: string
    local_ai_model: string
    gemini_model: string
    deepseek_model: string
    ai_prompt: string
    movie_age_threshold: string
    show_age_threshold: string
    fix_overlapping_subtitles: string
    strip_subtitle_formatting: string
    add_translator_info: string
    theme: ITheme
    locale: ILocale
    subtitle_validation_enabled: string
    subtitle_validation_maxfilesizebytes: string
    subtitle_validation_minsubtitlelength: string
    subtitle_validation_maxsubtitlelength: string
    subtitle_validation_mindurationms: string
    subtitle_validation_maxdurationsecs: string
    ai_context_prompt_enabled: string
    ai_context_prompt: string
    ai_context_before: string
    ai_context_after: string
    use_batch_translation: string
    max_batch_size: string
    use_subtitle_tagging: string
    remove_language_tag: string
    subtitle_tag: string
    ignore_captions: string
    max_retries: string
    retry_delay: string
    retry_delay_multiplier: string
    auth_enabled: string
    onboarding_completed: string
    telemetry_enabled: string
    navigate_to_details_on_request: string
    openai_request_template: string
    anthropic_request_template: string
    local_ai_chat_request_template: string
    local_ai_generate_request_template: string
    deepseek_request_template: string
    gemini_request_template: string
}


export const ENCRYPTED_SETTINGS = {
    API_KEY: 'api_key',
    RADARR_API_KEY: 'radarr_api_key',
    SONARR_API_KEY: 'sonarr_api_key',
    OPENAI_API_KEY: 'openai_api_key',
    ANTHROPIC_API_KEY: 'anthropic_api_key',
    GEMINI_API_KEY: 'gemini_api_key',
    DEEPSEEK_API_KEY: 'deepseek_api_key',
    DEEPL_API_KEY: 'deepl_api_key',
    LIBRETRANSLATE_API_KEY: 'libretranslate_api_key',
    LOCAL_AI_API_KEY: 'local_ai_api_key',
} as const

export interface IEncryptedSettings {
    api_key: string
    radarr_api_key: string
    sonarr_api_key: string
    openai_api_key: string
    anthropic_api_key: string
    gemini_api_key: string
    deepseek_api_key: string
    deepl_api_key: string
    libretranslate_api_key: string
    local_ai_api_key: string
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

export interface IFilterOptions {
    logLevel: string
}

export interface ILogEntry {
    logLevel: string
    message: string
    formattedTime: string
    formattedDate: string
    formattedSource: string
    category: string
    stackTrace?: string
}
