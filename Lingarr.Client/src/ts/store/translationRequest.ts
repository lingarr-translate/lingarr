import { IFilter, IPagedResult, ITranslationRequest } from '@/ts'

export interface IUseTranslationRequestStore {
    activeTranslationRequests: number
    translationRequests: IPagedResult<ITranslationRequest>
    filter: IFilter
    selectedRequests: ITranslationRequest[]
    selectAll: boolean
}
