import { ISubtitle } from '@/ts'

interface IBaseEntity {
    id: number
    createdAt: Date
    updatedAt: Date
}

export interface IShow extends IBaseEntity {
    sonarrId: number
    title: string
    path: string
    dateAdded?: Date | null
    images: IImage[]
    seasons: ISeason[]
    excludeFromTranslation: string
    translationAgeThreshold: string
}

export interface IMovie extends IBaseEntity {
    radarrId: number
    title: string
    fileName: string
    path: string
    dateAdded?: Date | null
    images: IImage[]
    subtitles?: ISubtitle[]
    excludeFromTranslation: string
    translationAgeThreshold: string
}

export interface ITranslationRequest {
    id: number
    jobId: string
    title: string
    sourceLanguage: string
    targetLanguage: string
    subtitleToTranslate?: string
    translatedSubtitle?: string
    mediaType: MediaType
    status: TranslationStatus
    progress: number
    completedAt?: string | null
    errorMessage?: string | null
    stackTrace?: string | null
    createdAt?: string
}

export interface ITranslationRequestDetail extends ITranslationRequest {
    events: ITranslationRequestEvent[]
    lines: ISubtitleLineComparison[]
}

export interface ITranslationRequestEvent {
    id: number
    status: TranslationStatus
    message?: string | null
    createdAt: string
}

export interface ISubtitleLineComparison {
    position: number
    source: string
    target: string
}

export interface ILineTranslated {
    id: number
    position: number
    source: string
    target: string
}

export interface IRequestProgress {
    id: number
    jobId: string
    status: TranslationStatus
    progress: number
    completed: boolean
    completedAt?: string | null
    errorMessage?: string | null
    stackTrace?: string | null
}

export interface IImage {
    id: number
    type: string
    path: string
    showId?: number | null
    show?: IShow | null
    movieId?: number | null
    movie?: IMovie | null
}

export interface ISeason extends IBaseEntity {
    seasonNumber: number
    path: string
    episodes: IEpisode[]
    showId: number
    show: IShow
    excludeFromTranslation: string
}

export interface IEpisode extends IBaseEntity {
    sonarrId: number
    episodeNumber: number
    title: string
    fileName?: string | null
    path?: string | null
    seasonId: number
    season: ISeason
    excludeFromTranslation: string
}

export interface IPagedResult<T> {
    items: T[]
    totalCount: number
    pageNumber: number
    pageSize: number
}

export const MEDIA_TYPE = {
    MOVIE: 'Movie',
    SHOW: 'Show',
    SEASON: 'Season',
    EPISODE: 'Episode'
} as const

export type MediaType = (typeof MEDIA_TYPE)[keyof typeof MEDIA_TYPE]

export const TRANSLATION_STATUS = {
    PENDING: 'Pending',
    INPROGRESS: 'InProgress',
    COMPLETED: 'Completed',
    FAILED: 'Failed',
    CANCELLED: 'Cancelled',
    INTERRUPTED: 'Interrupted'
} as const

export type TranslationStatus = (typeof TRANSLATION_STATUS)[keyof typeof TRANSLATION_STATUS]

export enum TRANSLATION_ACTIONS {
    CANCEL,
    REMOVE,
    RETRY
}
