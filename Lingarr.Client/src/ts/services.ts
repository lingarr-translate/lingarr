import {
    DirectoryItem,
    ILanguage,
    ISettings,
    ISubtitle,
    ITranslationRequest,
    MediaType
} from '@/ts'
import { IPathMapping } from '@/ts/index'

export interface Services {
    setting: ISettingService
    subtitle: ISubtitleService
    translate: ITranslateService
    translationRequest: ITranslationRequestService
    version: IVersionService
    media: IMediaService
    schedule: IScheduleService
    mapping: IMappingService
    directory: IDirectoryService
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
    getSettings<T>(keys: string[]): Promise<T>
    setSetting(key: string, value: string): Promise<void>
    setSettings(keys: ISettings): Promise<void>
}

export interface ISubtitleService {
    collect<T>(path: string): Promise<T>
}

export interface IVersionService {
    getVersion<T>(): Promise<T>
}

export interface ITranslateService {
    translateSubtitle<T>(
        mediaId: number,
        subtitle: ISubtitle,
        source: string,
        target: ILanguage,
        mediaType: MediaType
    ): Promise<T>
    getLanguages<T>(): Promise<T>
}

export interface ITranslationRequestService {
    getActiveCount<T>(): Promise<T>
    requests<T>(
        pageNumber: number,
        searchQuery: string,
        sortBy: string,
        ascending: boolean
    ): Promise<T>
    cancel<T>(translationRequest: ITranslationRequest): Promise<T>
}

export interface IScheduleService {
    remove<T>(jobId: string): Promise<T>
    reindex<T>(): Promise<T>
}

export interface IMappingService {
    getMappings(): Promise<IPathMapping[]>
    setMappings(mappings: IPathMapping[]): Promise<void>
}

export interface IDirectoryService {
    get(path: string): Promise<DirectoryItem[]>
}
