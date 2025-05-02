import { acceptHMRUpdate, defineStore } from 'pinia'
import {
    IUseSettingStore,
    ISettings,
    SETTINGS,
    ILanguage,
    SERVICE_TYPE,
    ICustomAiParams
} from '@/ts'
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
                // When Anthropic is selected the max_tokens parameter is required
                if (key === SETTINGS.SERVICE_TYPE && value === SERVICE_TYPE.ANTHROPIC) {
                    const maxTokensExists = this.settings.custom_ai_parameters as ICustomAiParams[]

                    if (!maxTokensExists.some((param) => param.key === 'max_tokens')) {
                        const updatedParams = [
                            ...this.settings.custom_ai_parameters,
                            { key: 'max_tokens', value: '1024' }
                        ]
                        this.storeSetting(SETTINGS.CUSTOM_AI_PARAMETERS, updatedParams)
                        await this.saveSetting(
                            SETTINGS.CUSTOM_AI_PARAMETERS,
                            JSON.stringify(updatedParams)
                        )
                    }
                }
                if (key === SETTINGS.SERVICE_TYPE) {
                    // When a new service is selected we check if all selected languages is still valid
                    const currentSourceLanguages = [
                        ...((this.settings[SETTINGS.SOURCE_LANGUAGES] as ILanguage[]) || [])
                    ]
                    const currentTargetLanguages = [
                        ...((this.settings[SETTINGS.TARGET_LANGUAGES] as ILanguage[]) || [])
                    ]

                    const translateStore = useTranslateStore()
                    await translateStore.setLanguages()
                    const newLanguages = translateStore.getLanguages

                    // Filter source languages that still exist in the new languages
                    const validSourceLanguages = currentSourceLanguages.filter((currentLang) =>
                        newLanguages.some(
                            (newLang) =>
                                newLang.code === currentLang.code &&
                                newLang.name === currentLang.name
                        )
                    )

                    // Filter target languages that still exist in the new languages and in a source language's targets
                    const validTargetLanguages = currentTargetLanguages.filter((currentTarget) => {
                        const matchingNewLang = newLanguages.find(
                            (newLang) =>
                                newLang.code === currentTarget.code &&
                                newLang.name === currentTarget.name
                        )

                        if (!matchingNewLang) return false
                        return newLanguages.some((newLang) =>
                            newLang.targets?.includes(currentTarget.code)
                        )
                    })

                    this.storeSetting(SETTINGS.SOURCE_LANGUAGES, validSourceLanguages)
                    this.storeSetting(SETTINGS.TARGET_LANGUAGES, validTargetLanguages)
                    await this.saveSetting(
                        SETTINGS.SOURCE_LANGUAGES,
                        JSON.stringify(validSourceLanguages)
                    )
                    await this.saveSetting(
                        SETTINGS.TARGET_LANGUAGES,
                        JSON.stringify(validTargetLanguages)
                    )
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
