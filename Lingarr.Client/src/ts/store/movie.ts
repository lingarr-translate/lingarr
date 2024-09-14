import { IFilter, IMovie, IPagedResult } from '@/ts'

export interface IUseMovieStore {
    movies: IPagedResult<IMovie>
    filter: IFilter
}
