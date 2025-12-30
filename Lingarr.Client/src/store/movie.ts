import { acceptHMRUpdate, defineStore } from 'pinia'
import { IFilter, IIncludeSummary, IMovie, IPagedResult, IUseMovieStore, MediaType, MEDIA_TYPE } from '@/ts'
import services from '@/services'

export const useMovieStore = defineStore('movie', {
    state: (): IUseMovieStore => ({
        movies: {
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
        getFilter: (state: IUseMovieStore): IFilter => state.filter,
        get(): IPagedResult<IMovie> {
            this.movies.items = this.movies.items?.map((item) => {
                return {
                    ...item,
                    subtitles: item.subtitles
                        ?.slice()
                        .sort((a, b) => a.language.localeCompare(b.language))
                }
            })
            return this.movies
        }
    },
    actions: {
        async setFilter(filterVal: IFilter) {
            this.filter = filterVal.searchQuery ? { ...filterVal, pageNumber: 1 } : filterVal
            await this.fetch()
        },
        async fetch() {
            this.movies = await services.media.movies(
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
            await services.media.includeAll(MEDIA_TYPE.MOVIE, include)
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
    import.meta.hot.accept(acceptHMRUpdate(useMovieStore, import.meta.hot))
}
