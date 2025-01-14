import { ComputedRef } from 'vue'

export interface Translations {
    [key: string]: string | Translations
}

export interface Language {
    code: string
    name: string
}

export interface I18nPluginOptions {
    defaultLocale?: string
}

export interface I18n {
    translate: (key: string) => string
    loadTranslations: () => Promise<void>
    setLocale: (locale: string) => Promise<void>
    locale: ComputedRef<string>
    languages: ComputedRef<Language[]>
}
