import { acceptHMRUpdate, defineStore } from 'pinia'
import { IUseSettingStore, ISettings, SETTINGS, ILanguage } from '@/ts'
import services from '@/services'

export const useSettingStore = defineStore({
    id: 'setting',
    state: (): IUseSettingStore => ({
        settings: {} as ISettings
    }),
    getters: {
        getSettings: (state: IUseSettingStore): ISettings => {
            return {
                ...state.settings,
                source_languages: JSON.parse(state.settings.source_languages as string),
                target_languages: JSON.parse(state.settings.target_languages as string)
            }
        },
        getSetting: (state: IUseSettingStore) => (key: keyof ISettings) => state.settings[key]
    },
    actions: {
        updateSetting(key: keyof ISettings, value: string | ILanguage[]) {
            if (['radarr_url', 'sonarr_url'].includes(key)) {
                value = (value as string).replace(/\/+$/, '')
            }

            if (key === 'source_languages' || key === 'target_languages') {
                this.settings[key] = value as ILanguage[]
                this.saveSetting(key, JSON.stringify(value))
            } else {
                this.settings[key] = value as string
                this.saveSetting(key, value as string)
            }
        },

        saveSetting(key: keyof ISettings, value: string) {
            services.setting.setSetting(key, value)
        },

        async applySettingsOnLoad(): Promise<void> {
            const settings = await services.setting.getSettings<ISettings>(Object.values(SETTINGS))

            this.settings = {
                ...settings,
                source_languages: JSON.parse(settings.source_languages as string),
                target_languages: JSON.parse(settings.target_languages as string)
            }
        }
    }
})

if (import.meta.hot) {
    import.meta.hot.accept(acceptHMRUpdate(useSettingStore, import.meta.hot))
}
