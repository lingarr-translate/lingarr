import { acceptHMRUpdate, defineStore } from 'pinia'
import {
    IFilter,
    IPagedResult,
    IRequestProgress,
    ITranslationRequest,
    IUseTranslationRequestStore
} from '@/ts'
import services from '@/services'

export const useTranslationRequestStore = defineStore({
    id: 'translateRequest',
    state: (): IUseTranslationRequestStore => ({
        activeTranslationRequests: 0,
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
        }
    }),
    getters: {
        getActiveTranslationRequests: (state: IUseTranslationRequestStore): number =>
            state.activeTranslationRequests,
        getTranslationRequests(): IPagedResult<ITranslationRequest> {
            return this.translationRequests
        },
        getFilter: (state: IUseTranslationRequestStore): IFilter => state.filter
    },
    actions: {
        async setFilter(filterVal: IFilter) {
            this.filter = filterVal.searchQuery ? { ...filterVal, pageNumber: 1 } : filterVal
            await this.fetch()
        },
        async fetch() {
            this.translationRequests = await services.translationRequest.requests<
                IPagedResult<ITranslationRequest>
            >(
                this.filter.pageNumber,
                this.filter.searchQuery,
                this.filter.sortBy,
                this.filter.isAscending
            )
        },
        async setActiveCount(activeTranslationRequests: number) {
            this.activeTranslationRequests = activeTranslationRequests
        },
        async getActiveCount() {
            const activeTranslationRequests =
                await services.translationRequest.getActiveCount<number>()
            await this.setActiveCount(activeTranslationRequests)
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
        async updateProgress(requestProgress: IRequestProgress) {
            this.translationRequests.items = this.translationRequests.items.map(
                (request: ITranslationRequest) => {
                    if (request.id === requestProgress.id) {
                        return {
                            ...request,
                            status: requestProgress.status,
                            progress: requestProgress.progress,
                            completedAt: requestProgress.completedAt
                        }
                    }
                    return request
                }
            )
        }
    }
})

if (import.meta.hot) {
    import.meta.hot.accept(acceptHMRUpdate(useTranslationRequestStore, import.meta.hot))
}
