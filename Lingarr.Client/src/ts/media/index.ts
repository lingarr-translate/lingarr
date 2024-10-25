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
}

export interface IMovie extends IBaseEntity {
    radarrId: number
    title: string
    fileName: string
    path: string
    dateAdded?: Date | null
    images: IImage[]
    subtitles?: ISubtitle[]
}

export interface IMedia {
    id: number
    title: string
    episodeNumber?: string
    seasonNumber?: string
    showTitle?: string
}

export interface ITranslationRequest extends IBaseEntity {
    jobId: string
    mediaType: MediaType
    media: IMedia
    sourceLanguage: string
    targetLanguage: string
    translatedSubtitle?: string
    subtitleToTranslate?: string
    status: TranslationStatus
    progress?: number | null
    completedAt?: Date | null
}

export interface IRequestProgress {
    id: number
    jobId: string
    status: TranslationStatus
    progress: number
    completed: boolean
    completedAt?: Date | null
}

export type IProgressMap = Map<number, IRequestProgress>

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
}

export interface IEpisode extends IBaseEntity {
    sonarrId: number
    episodeNumber: number
    title: string
    fileName?: string | null
    path?: string | null
    seasonId: number
    season: ISeason
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
    EPISODE: 'Episode'
} as const

export type MediaType = (typeof MEDIA_TYPE)[keyof typeof MEDIA_TYPE]

export const TRANSLATION_STATUS = {
    PENDING: 'Pending',
    INPROGRESS: 'In Progress',
    COMPLETED: 'Completed',
    FAILED: 'Failed',
    CANCELLED: 'Cancelled'
} as const

export type TranslationStatus = (typeof TRANSLATION_STATUS)[keyof typeof TRANSLATION_STATUS]
