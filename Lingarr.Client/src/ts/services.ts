import {
    DirectoryItem,
    ILanguage,
    ISettings,
    ISubtitle,
    ITranslationRequest,
    MediaType,
    IPathMapping,
    IOnboardingRequest,
    ISignupRequest,
    ILoginRequest,
    IApiKeyResponse,
    IUser,
    IUpdateUserRequest,
    IIncludeSummary
} from '@/ts'

export interface Services {
    auth: IAuthService
    setting: ISettingService
    subtitle: ISubtitleService
    translate: ITranslateService
    translationRequest: ITranslationRequestService
    version: IVersionService
    media: IMediaService
    schedule: IScheduleService
    mapping: IMappingService
    directory: IDirectoryService
    statistics: IStatisticsService
    logs: ILogsService
    telemetry: ITelemetryService
}

export interface IAuthService {
    completeOnboarding(request: IOnboardingRequest): Promise<void>
    authenticated(): Promise<void>
    signup(request: ISignupRequest): Promise<void>
    login(request: ILoginRequest): Promise<void>
    logout(): Promise<void>
    generateApiKey(): Promise<IApiKeyResponse>
    hasAnyUsers(): Promise<boolean>
    getUsers(): Promise<IUser[]>
    updateUser(id: number, request: IUpdateUserRequest): Promise<void>
    deleteUser(id: number): Promise<void>
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
    include<T>(mediaType: MediaType, id: number, include: boolean): Promise<T>
    includeAll<T>(mediaType: MediaType, include: boolean): Promise<T>
    exclude<T>(mediaType: MediaType, id: number): Promise<T>
    threshold<T>(mediaType: MediaType, id: number, hours: string): Promise<T>
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
    getModels<T>(): Promise<T>
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
    remove<T>(translationRequest: ITranslationRequest): Promise<T>
    retry<T>(translationRequest: ITranslationRequest): Promise<T>
}

export interface IScheduleService {
    startJob<T>(jobName: string): Promise<T>
    recurringJobs<T>(): Promise<T>
    remove<T>(jobId: string): Promise<T>
    indexShows<T>(): Promise<T>
    indexMovies<T>(): Promise<T>
}

export interface IMappingService {
    getMappings(): Promise<IPathMapping[]>
    setMappings(mappings: IPathMapping[]): Promise<void>
}

export interface IDirectoryService {
    get(path: string): Promise<DirectoryItem[]>
}

export interface IStatisticsService {
    getStatistics<T>(): Promise<T>
    getDailyStatistics<T>(days?: number): Promise<T>
    resetStatistics(): Promise<void>
}

export interface ILogsService {
    getStream(): EventSource
}

export interface ITelemetryService {
    preview<T>(): Promise<T>
}
