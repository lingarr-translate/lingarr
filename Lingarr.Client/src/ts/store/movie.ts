import { IFilter, IIncludeSummary, IMovie, IPagedResult } from '@/ts'

export interface IUseMovieStore {
    movies: IPagedResult<IMovie>
    filter: IFilter
    selectedMovies: IMovie[]
    selectAll: boolean
    includeSummary: IIncludeSummary
}
