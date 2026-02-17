import { IFilter, IIncludeSummary, IPagedResult, IShow } from '@/ts'

export interface IUseShowStore {
    shows: IPagedResult<IShow>
    filter: IFilter
    selectedShows: IShow[]
    selectAll: boolean
    includeSummary: IIncludeSummary
}
