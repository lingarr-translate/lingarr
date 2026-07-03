import { acceptHMRUpdate, defineStore } from 'pinia'
import {
    IEncryptedSettings,
    IPluginSettingField,
    ISettings,
    IUseSettingStore,
    ENCRYPTED_SETTINGS,
    PLUGIN_SETTING_TYPE,
    SETTINGS
} from '@/ts'
import services from '@/services'
import { useInstanceStore } from '@/store/instance'

export const useSettingStore = defineStore('setting', {
    state: (): IUseSettingStore => ({
        settings: {} as ISettings,
        encrypted_settings: {} as IEncryptedSettings
    }),
    getters: {
        getSettings: (state: IUseSettingStore): ISettings => {
            return {
                ...state.settings,
                source_languages: JSON.parse(state.settings.source_languages as string),
                target_languages: JSON.parse(state.settings.target_languages as string)
            }
        },
        getSetting: (state: IUseSettingStore) => (key: keyof ISettings) => state.settings[key],
        getEncryptedSetting: (state: IUseSettingStore) => (key: keyof IEncryptedSettings) => state.encrypted_settings[key]
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
            }
        },
        async updateEncryptedSetting(
            key: keyof IEncryptedSettings,
            value: string,
            isValid: boolean = true
        ): Promise<void> {
            this.encrypted_settings[key] = value
            if (isValid) {
                await services.setting.setEncryptedSetting(key, value)
            }
        },
        storeSetting(key: keyof ISettings, value: string | boolean | unknown[]) {
            this.settings[key] = value as never
        },
        async saveSetting(key: keyof ISettings, value: string) {
            await services.setting.setSetting(key, value)
        },

        async setPluginSettings(fields: IPluginSettingField[]): Promise<void> {
            const plainKeys: string[] = []
            const encryptedKeys: string[] = []
            for (const field of fields) {
                if (field.type === PLUGIN_SETTING_TYPE.SECRET) {
                    encryptedKeys.push(field.key)
                } else {
                    plainKeys.push(field.key)
                }
            }

            const [plainValues, encryptedValues] = await Promise.all([
                plainKeys.length > 0
                    ? services.setting.getSettings<Partial<ISettings>>(plainKeys)
                    : Promise.resolve({} as Partial<ISettings>),
                encryptedKeys.length > 0
                    ? services.setting.getEncryptedSettings<Partial<IEncryptedSettings>>(encryptedKeys)
                    : Promise.resolve({} as Partial<IEncryptedSettings>)
            ])

            this.settings = { ...this.settings, ...plainValues }
            this.encrypted_settings = { ...this.encrypted_settings, ...encryptedValues }
        },

        async applySettingsOnLoad(): Promise<void> {
            const instanceStore = useInstanceStore()
            const [settings, encryptedSettings] = await Promise.all([
                services.setting.getSettings<ISettings>(Object.values(SETTINGS)),
                services.setting.getEncryptedSettings<IEncryptedSettings>(Object.values(ENCRYPTED_SETTINGS))
            ])

            this.settings = {
                ...settings,
                source_languages: JSON.parse(settings.source_languages as string),
                target_languages: JSON.parse(settings.target_languages as string)
            }
            this.encrypted_settings = encryptedSettings

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
