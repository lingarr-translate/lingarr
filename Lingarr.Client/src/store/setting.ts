import { acceptHMRUpdate, defineStore } from 'pinia'
import { IUseSettingStore, ISettings } from '@/ts'
import services from '@/services'

export const useSettingStore = defineStore({
    id: 'setting',
    state: (): IUseSettingStore => ({
        settings: {
            radarr_api_key: '',
            radarr_url: '',
            sonarr_api_key: '',
            sonarr_url: '',
            theme: '',
            source_languages: '',
            target_languages: ''
        }
    }),
    getters: {
        getSettings: (state): ISettings => {
            return {
                ...state.settings,
                source_languages: JSON.parse(state.settings.source_languages as string),
                target_languages: JSON.parse(state.settings.target_languages as string)
            }
        },
        getSetting: (state: IUseSettingStore) => (key: keyof ISettings) => state.settings[key]
    },
    actions: {
        updateSetting(key: keyof ISettings, value: string) {
            let setting = value
            if(['radarr_url','sonarr_url'].includes(key)) {
                setting = setting.replace(/\/+$/, '')
            }
            this.settings[key] = setting
            this.saveSettings()
        },
        saveSettings() {
            services.setting.setSettings({
                ...this.settings,
                source_languages: JSON.stringify(this.settings.source_languages),
                target_languages: JSON.stringify(this.settings.target_languages)
            })
        },
        async applySettingsOnLoad(): Promise<void> {
            const settings = await services.setting.getSettings<ISettings>([
                'theme',
                'radarr_url',
                'radarr_api_key',
                'sonarr_url',
                'sonarr_api_key',
                'source_languages',
                'target_languages'
            ])

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
