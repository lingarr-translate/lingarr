import { ILanguage } from '@/ts'

export interface IUseSettingStore {
    settings: ISettings
}

export interface ISettings {
    radarr_api_key: string
    radarr_url: string
    sonarr_api_key: string
    sonarr_url: string
    theme: string
    source_languages: string | ILanguage[]
    target_languages: string | ILanguage[]
    sonarr_settings_completed: string
    radarr_settings_completed: string
}
