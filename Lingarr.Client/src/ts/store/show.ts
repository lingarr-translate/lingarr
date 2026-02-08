import { IFilter, IPagedResult, ISeason, IShow } from '@/ts'

export interface IUseShowStore {
    shows: IPagedResult<IShow>
    filter: IFilter
    selectedShows: IShow[]
    selectedSeasons: Record<number, ISeason>
    selectAll: boolean
}
