import { IActiveTranslation, IFilter, IPagedResult, ITranslationRequest } from '@/ts'
import { SelectionMode } from '@/utils/selection'

export interface IUseTranslationRequestStore {
    activeTranslations: IActiveTranslation[]
    translationRequests: IPagedResult<ITranslationRequest>
    filter: IFilter
    selectedRequests: ITranslationRequest[]
    selectionMode: SelectionMode
    excludeIds: number[]
    proofreadSupported: boolean
}
