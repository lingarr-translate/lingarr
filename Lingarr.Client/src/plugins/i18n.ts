import { App, ref, Ref, computed, inject, InjectionKey, ComputedRef } from 'vue'
import { useLocalStorage } from '@/composables/useLocalStorage'

// Types
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

export const I18nInjectionKey: InjectionKey<I18n> = Symbol('i18n')

export function createI18nPlugin(options: I18nPluginOptions = {}) {
    const localStorage = useLocalStorage()
    const currentLocale: Ref<string> = ref(
        localStorage.getItem('locale') || options.defaultLocale || 'en'
    )
    const messages = ref<Record<string, Translations>>({})
    const availableLanguages: Ref<Language[]> = ref<Language[]>([])

    const translate = (key: string): string => {
        const keys = key.split('.')
        let value: string | Translations = messages.value[currentLocale.value] || {}

        for (const k of keys) {
            if (typeof value === 'object' && k in value) {
                value = value[k]
            } else {
                console.warn(`Translation key not found: ${key}`)
                return key
            }
        }

        return typeof value === 'string' ? value : key
    }

    const loadTranslations = async () => {
        try {
            const response = await fetch(`/api/translations/languages`)
            availableLanguages.value = await response.json()
            await loadLocaleMessages(currentLocale.value)
        } catch (error) {
            console.error('Failed to load translations:', error)
        }
    }

    const loadLocaleMessages = async (locale: string) => {
        try {
            const response = await fetch(`/api/translations/${locale}`)
            messages.value[locale] = await response.json()
        } catch (error) {
            console.error(`Failed to load messages for locale ${locale}:`, error)
        }
    }

    const setLocale = async (locale: string) => {
        if (!messages.value[locale]) {
            await loadLocaleMessages(locale)
        }
        currentLocale.value = locale
        localStorage.setItem('locale', locale)
        document.querySelector('html')?.setAttribute('lang', locale)
    }

    const i18n: I18n = {
        translate,
        loadTranslations,
        setLocale,
        locale: computed(() => currentLocale.value),
        languages: computed(() => availableLanguages.value)
    }

    const plugin = {
        install(app: App) {
            // injection for i18n
            app.provide(I18nInjectionKey, i18n)

            // helper for translations
            app.config.globalProperties.translate = translate

            // directive for translations
            app.directive('translate', {
                mounted(el, binding) {
                    el.textContent = translate(binding.value)
                },
                updated(el, binding) {
                    el.textContent = translate(binding.value)
                }
            })
        }
    }

    return { plugin, i18n }
}

// Composable for use in components
export function useI18n(): I18n {
    const i18n = inject(I18nInjectionKey)
    if (!i18n) throw new Error('i18n plugin not installed')
    return i18n
}
