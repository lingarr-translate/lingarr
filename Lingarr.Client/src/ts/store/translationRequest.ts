import { IFilter, IPagedResult, IProgressMap, ITranslationRequest } from '@/ts'

export interface IUseTranslationRequestStore {
    activeTranslationRequests: number
    translationRequests: IPagedResult<ITranslationRequest>
    progressMap: IProgressMap
    filter: IFilter
}
