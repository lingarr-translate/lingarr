import { IActiveTranslation, IFilter, IPagedResult, ITranslationRequest } from '@/ts'

export interface IUseTranslationRequestStore {
    activeTranslations: IActiveTranslation[]
    translationRequests: IPagedResult<ITranslationRequest>
    filter: IFilter
    selectedRequests: ITranslationRequest[]
    selectAll: boolean
}
