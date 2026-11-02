import { acceptHMRUpdate, defineStore } from 'pinia'
import {
    IActiveTranslation,
    IFilter,
    IPagedResult,
    IProofreadLineApplyRequest,
    IProofreadStatus,
    IRequestProgress,
    ITranslationRequest,
    IUseTranslationRequestStore,
    TRANSLATION_STATUS,
    TranslationStatus
} from '@/ts'
import services from '@/services'
import {
    emptySelection,
    isItemSelected,
    isPageSelected as pageIsSelected,
    selectionCount as countSelection,
    selectAllMatching as matchingSelection,
    SelectionState,
    toggleItem,
    togglePage
} from '@/utils/selection'

function currentSelection(state: IUseTranslationRequestStore): SelectionState<ITranslationRequest> {
    return {
        mode: state.selectionMode,
        selected: state.selectedRequests,
        excludeIds: state.excludeIds
    }
}

function sameStatuses(left?: TranslationStatus[], right?: TranslationStatus[]) {
    const current = left ?? []
    const next = right ?? []
    return current.length === next.length && current.every((status, index) => status === next[index])
}

export const useTranslationRequestStore = defineStore('translateRequest', {
    state: (): IUseTranslationRequestStore => ({
        activeTranslations: [],
        translationRequests: {
            totalCount: 0,
            pageSize: 0,
            pageNumber: 0,
            items: []
        },
        filter: {
            searchQuery: '',
            sortBy: 'CreatedAt',
            isAscending: true,
            pageNumber: 1
        },
        selectedRequests: [] as ITranslationRequest[],
        selectionMode: 'ids',
        excludeIds: [],
        proofreadSupported: false
    }),
    getters: {
        getActiveTranslationCount: (state: IUseTranslationRequestStore): number =>
            state.activeTranslations.length,
        getTranslationRequests(): IPagedResult<ITranslationRequest> {
            return this.translationRequests
        },
        getFilter: (state: IUseTranslationRequestStore): IFilter => state.filter,
        getSelectedRequests: (state: IUseTranslationRequestStore): ITranslationRequest[] =>
            state.selectedRequests,
        isPageSelected(): boolean {
            return pageIsSelected(currentSelection(this), this.translationRequests.items)
        },
        selectionCount(): number {
            return countSelection(currentSelection(this), this.translationRequests.totalCount)
        },
        isSelected() {
            return (id: number) => isItemSelected(currentSelection(this), id)
        }
    },
    actions: {
        async setFilter(filterVal: IFilter) {
            const statusesChanged = !sameStatuses(this.filter.statuses, filterVal.statuses)
            if (filterVal.searchQuery !== this.filter.searchQuery || statusesChanged) {
                this.clearSelection()
            }
            const next = statusesChanged ? { ...filterVal, pageNumber: 1 } : filterVal
            this.filter = next.searchQuery ? { ...next, pageNumber: 1 } : next
            await this.fetch()
        },
        async fetch() {
            this.translationRequests = await services.translationRequest.requests<
                IPagedResult<ITranslationRequest>
            >(
                this.filter.pageNumber,
                this.filter.searchQuery,
                this.filter.sortBy,
                this.filter.isAscending,
                this.filter.statuses
            )
        },
        setActiveTranslations(activeTranslations: IActiveTranslation[]) {
            this.activeTranslations = activeTranslations
        },
        async fetchActiveTranslations() {
            this.activeTranslations =
                await services.translationRequest.getActiveTranslations<IActiveTranslation[]>()
        },
        async cancel(translationRequest: ITranslationRequest) {
            await services.translationRequest.cancel<string>(translationRequest)
        },
        async remove(translationRequest: ITranslationRequest) {
            await services.translationRequest.remove<string>(translationRequest).finally(() => {
                this.translationRequests.items = this.translationRequests.items.filter(
                    (request) => request.id !== translationRequest.id
                )
            })
        },
        async retry(translationRequest: ITranslationRequest) {
            await services.translationRequest.retry<string>(translationRequest)
            await this.fetch()
        },
        async resume(translationRequest: ITranslationRequest) {
            await services.translationRequest.resume<string>(translationRequest)
            await this.fetch()
        },
        async proofread(translationRequest: ITranslationRequest) {
            await services.translationRequest.proofread<string>(translationRequest)
            await this.fetch()
        },
        async applyProofreadLine(request: IProofreadLineApplyRequest) {
            return await services.translationRequest.applyProofreadLine<string>(request)
        },
        async fetchProofreadStatus() {
            const status = await services.translate.proofreadStatus<IProofreadStatus>()
            this.proofreadSupported = status.supported
        },
        async updateProgress(requestProgress: IRequestProgress) {
            const completionStatuses: TranslationStatus[] = [
                TRANSLATION_STATUS.CANCELLED,
                TRANSLATION_STATUS.FAILED,
                TRANSLATION_STATUS.COMPLETED,
                TRANSLATION_STATUS.INTERRUPTED
            ]

            this.translationRequests.items = this.translationRequests.items.map(
                (request: ITranslationRequest) => {
                    if (request.id === requestProgress.id) {
                        if (
                            completionStatuses.includes(request.status) &&
                            !completionStatuses.includes(requestProgress.status)
                        ) {
                            return request
                        }
                        return {
                            ...request,
                            status: requestProgress.status,
                            progress: requestProgress.progress,
                            completedAt: requestProgress.completedAt,
                            errorMessage: requestProgress.errorMessage ?? request.errorMessage,
                            stackTrace: requestProgress.stackTrace ?? request.stackTrace
                        }
                    }
                    return request
                }
            )
        },
        async removeSelected() {
            if (this.selectionMode === 'query') {
                await services.translationRequest.removeBulk<number>({
                    allMatching: true,
                    searchQuery: this.filter.searchQuery,
                    statuses: this.filter.statuses,
                    excludeIds: this.excludeIds
                })
            } else {
                await services.translationRequest.removeBulk<number>({
                    ids: this.selectedRequests.map((request) => request.id)
                })
            }
            this.clearSelection()
            await this.fetch()
        },
        applySelection(next: SelectionState<ITranslationRequest>) {
            this.selectionMode = next.mode
            this.selectedRequests = next.selected
            this.excludeIds = next.excludeIds
        },
        clearSelection() {
            this.applySelection(emptySelection())
        },
        toggleSelectAll() {
            this.applySelection(togglePage(currentSelection(this), this.translationRequests.items))
        },
        selectAllMatching() {
            this.applySelection(matchingSelection())
        },
        toggleSelect(request: ITranslationRequest) {
            this.applySelection(toggleItem(currentSelection(this), request))
        }
    }
})
export default useTranslationRequestStore

if (import.meta.hot) {
    import.meta.hot.accept(acceptHMRUpdate(useTranslationRequestStore, import.meta.hot))
}
