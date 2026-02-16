import { IFilter, IPagedResult, IShow } from '@/ts'

export interface IUseShowStore {
    shows: IPagedResult<IShow>
    filter: IFilter
    selectedShows: IShow[]
    selectAll: boolean
}
