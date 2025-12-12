import { App, ref, Ref, computed, inject, InjectionKey } from 'vue'
import { I18n, I18nPluginOptions, Translations, Language } from '@/ts/plugins/i18n'
import { useLocalStorage } from '@/composables/useLocalStorage'
import services from '@/services'

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

    const loadTranslations = async (languages = true) => {
        if (languages) {
            const languagesResponse = await fetch(`/api/translation/languages`)
            availableLanguages.value = await languagesResponse.json()
        }
        const response = await fetch(`/api/translation`)
        const data = await response.json()
        messages.value = { [data.locale]: data.messages }
        // @todo remove all localisation
        currentLocale.value = 'en'
        localStorage.setItem('locale', 'en')
    }

    const setLocale = async (locale: string) => {
        await services.setting.setSetting('locale', locale)
        await loadTranslations(false)
        window.location.reload()
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
                    el.innerHTML = translate(binding.value)
                },
                updated(el, binding) {
                    el.innerHTML = translate(binding.value)
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
