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
    media: IMedia[]
    seasons: ISeason[]
}

export interface IMovie extends IBaseEntity {
    radarrId: number
    title: string
    fileName: string
    path: string
    dateAdded?: Date | null
    media: IMedia[]
    subtitles?: ISubtitle[]
}

export interface IMedia {
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
