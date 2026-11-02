import { IFilter, IIncludeSummary, IPagedResult, IShow } from '@/ts'
import { SelectionMode } from '@/utils/selection'

export interface IUseShowStore {
    shows: IPagedResult<IShow>
    filter: IFilter
    selectedShows: IShow[]
    selectionMode: SelectionMode
    excludeIds: number[]
    includeSummary: IIncludeSummary
}
