import { acceptHMRUpdate, defineStore } from 'pinia'
import { IUseSettingStore, ISettings, SETTINGS } from '@/ts'
import services from '@/services'
import { useTranslateStore } from '@/store/translate'
import { useInstanceStore } from '@/store/instance'

export const useSettingStore = defineStore({
    id: 'setting',
    state: (): IUseSettingStore => ({
        settings: {} as ISettings
    }),
    getters: {
        getSettings: (state: IUseSettingStore): ISettings => {
            console.log(state.settings)
            return {
                ...state.settings,
                source_languages: JSON.parse(state.settings.source_languages as string),
                target_languages: JSON.parse(state.settings.target_languages as string),
                custom_ai_parameters: JSON.parse(state.settings.custom_ai_parameters as string)
            }
        },
        getSetting: (state: IUseSettingStore) => (key: keyof ISettings) => state.settings[key]
    },
    actions: {
        async updateSetting(
            key: keyof ISettings,
            value: string | boolean | unknown[],
            isValid: boolean,
            isJson: boolean = false
        ): Promise<void> {
            if (typeof value === 'string' && (key === 'radarr_url' || key === 'sonarr_url')) {
                value = value.replace(/\/+$/, '')
            }

            this.storeSetting(key, value)
            if (isValid) {
                if (isJson) {
                    if (!allKeysHaveValues(value)) return
                    await this.saveSetting(key, JSON.stringify(value))
                } else {
                    await this.saveSetting(key, value as string)
                }
                if (key === SETTINGS.SERVICE_TYPE) {
                    const emptyLanguages = JSON.stringify([])
                    this.storeSetting(SETTINGS.SOURCE_LANGUAGES, [])
                    this.storeSetting(SETTINGS.TARGET_LANGUAGES, [])
                    await this.saveSetting(SETTINGS.SOURCE_LANGUAGES, emptyLanguages)
                    await this.saveSetting(SETTINGS.TARGET_LANGUAGES, emptyLanguages)
                    await useTranslateStore().setLanguages()
                }
            }
        },
        storeSetting(key: keyof ISettings, value: string | boolean | unknown[]) {
            this.settings[key] = value as never
        },
        async saveSetting(key: keyof ISettings, value: string) {
            await services.setting.setSetting(key, value)
        },

        async applySettingsOnLoad(): Promise<void> {
            const instanceStore = useInstanceStore()
            const settings = await services.setting.getSettings<ISettings>(Object.values(SETTINGS))

            this.settings = {
                ...settings,
                source_languages: JSON.parse(settings.source_languages as string),
                target_languages: JSON.parse(settings.target_languages as string),
                custom_ai_parameters: JSON.parse(settings.custom_ai_parameters as string)
            }

            instanceStore.storeTheme(settings.theme)
        }
    }
})

function allKeysHaveValues(values: string | boolean | unknown[]) {
    // If values is not an array, return false immediately
    if (!Array.isArray(values)) {
        return true
    }

    // Check if every item in the array is a valid object
    const isArrayOfObjects = values.every(
        (item): item is Record<string, unknown> =>
            item !== null && typeof item === 'object' && !Array.isArray(item)
    )

    if (!isArrayOfObjects) {
        return false
    }

    // Now TypeScript knows values is an array of objects
    return values.every((obj) => {
        return Object.values(obj).every(
            (value) => value !== null && value !== undefined && value !== ''
        )
    })
}

if (import.meta.hot) {
    import.meta.hot.accept(acceptHMRUpdate(useSettingStore, import.meta.hot))
}
