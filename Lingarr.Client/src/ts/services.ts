import { ISettings, ISubtitle } from '@/ts'

export interface Services {
    setting: ISettingService
    subtitle: ISubtitleService
    translate: ITranslateService
    media: IMediaService
}

export interface IMediaService {
    movies<T>(
        pageNumber: number,
        searchQuery: string,
        sortBy: string,
        ascending: boolean
    ): Promise<T>
    shows<T>(
        pageNumber: number,
        searchQuery: string,
        sortBy: string,
        ascending: boolean
    ): Promise<T>
}

export interface ISettingService {
    getSetting<T>(key: string): Promise<T>
    setSetting(key: string, value: string): void
    getSettings<T>(keys: string[]): Promise<T>
    setSettings(keys: ISettings): void
}

export interface ISubtitleService {
    collect<T>(path: string): Promise<T>
    translate<T>(subtitle: ISubtitle, target: string): Promise<T>
}

export interface ITranslateService {
    translateSubtitle<T>(subtitle: ISubtitle, target: string): Promise<T>
}
