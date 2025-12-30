import { acceptHMRUpdate, defineStore } from 'pinia'
import services from '@/services'
import { IFilter, IIncludeSummary, IUseShowStore, IPagedResult, IShow, MediaType, MEDIA_TYPE } from '@/ts'

export const useShowStore = defineStore('show', {
    state: (): IUseShowStore => ({
        shows: {
            totalCount: 0,
            pageSize: 0,
            pageNumber: 0,
            items: []
        },
        filter: {
            searchQuery: '',
            sortBy: 'Title',
            isAscending: true,
            pageNumber: 1
        }
    }),
    getters: {
        getFilter: (state: IUseShowStore): IFilter => state.filter,
        get: (state: IUseShowStore): IPagedResult<IShow> => state.shows
    },
    actions: {
        async setFilter(filterVal: IFilter) {
            this.filter = filterVal.searchQuery ? { ...filterVal, pageNumber: 1 } : filterVal
            await this.fetch()
        },
        async fetch() {
            this.shows = await services.media.shows(
                this.filter.pageNumber,
                this.filter.searchQuery,
                this.filter.sortBy,
                this.filter.isAscending
            )
        },
        async include(type: MediaType, id: number, include: boolean) {
            await services.media.include(type, id, include)
        },
        async includeAll(include: boolean) {
            await services.media.includeAll(MEDIA_TYPE.SHOW, include)
        },
        async fetchIncludeSummary(): Promise<IIncludeSummary> {
            return services.media.includeSummary()
        },
        async updateThreshold(type: MediaType, id: number, hours: string) {
            await services.media.threshold(type, id, hours)
        }
    }
})

if (import.meta.hot) {
    import.meta.hot.accept(acceptHMRUpdate(useShowStore, import.meta.hot))
}
