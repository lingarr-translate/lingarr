import { IFilter, IIncludeSummary, IMovie, IPagedResult } from '@/ts'
import { SelectionMode } from '@/utils/selection'

export interface IUseMovieStore {
    movies: IPagedResult<IMovie>
    filter: IFilter
    selectedMovies: IMovie[]
    selectionMode: SelectionMode
    excludeIds: number[]
    includeSummary: IIncludeSummary
}
