import { acceptHMRUpdate, defineStore } from 'pinia'
import { IFilter, IMovie, IPagedResult, IUseMovieStore, MediaType, MEDIA_TYPE } from '@/ts'
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
        },
        selectedMovies: [],
        selectAll: false
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
        },
        // Reactive computed summary from API response with global counts
        includeSummary(state: IUseMovieStore) {
            const total = state.movies.totalCount || 0
            const included = state.movies.includedCount ?? 0
            const excluded = state.movies.excludedCount ?? 0
            
            return {
                total,
                included,
                excluded,
                allIncluded: total > 0 && included === total
            }
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
            try {
                await services.media.include(type, id, include)
                // Update local state after successful API call
                const movie = this.movies.items?.find(m => m.id === id)
                if (movie) {
                    movie.excludeFromTranslation = !include
                }
            } catch (error) {
                console.error('Failed to update movie include state', error)
                throw error
            }
        },
        async includeAll(include: boolean) {
            try {
                await services.media.includeAll(MEDIA_TYPE.MOVIE, include)
                // Update all local movie states after successful API call
                this.movies.items?.forEach(movie => {
                    movie.excludeFromTranslation = !include
                })
            } catch (error) {
                console.error('Failed to update all movies include state', error)
                throw error
            }
        },
        async updateThreshold(type: MediaType, id: number, hours: string) {
            await services.media.threshold(type, id, hours)
        },
        clearSelection() {
            this.selectedMovies = []
            this.selectAll = false
        },
        toggleSelectAll() {
            if (this.selectAll) {
                this.selectedMovies = []
                this.selectAll = false
            } else {
                this.selectedMovies = [...this.movies.items]
                this.selectAll = true
            }
        },
        toggleSelect(movie: IMovie) {
            const index = this.selectedMovies.findIndex((m) => m.id === movie.id)
            if (index === -1) {
                this.selectedMovies.push(movie)
            } else {
                this.selectedMovies.splice(index, 1)
            }
            this.selectAll = this.selectedMovies.length === this.movies.items.length
        }
    }
})

if (import.meta.hot) {
    import.meta.hot.accept(acceptHMRUpdate(useMovieStore, import.meta.hot))
}
