import { acceptHMRUpdate, defineStore } from 'pinia'
import { IFilter, IMovie, IPagedResult, IUseMovieStore, MediaType } from '@/ts'
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

function currentSelection(state: IUseMovieStore): SelectionState<IMovie> {
    return {
        mode: state.selectionMode,
        selected: state.selectedMovies,
        excludeIds: state.excludeIds
    }
}

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
        selectionMode: 'ids',
        excludeIds: [],
        includeSummary: { included: 0, total: 0 }
    }),
    getters: {
        getFilter: (state: IUseMovieStore): IFilter => state.filter,
        isPageSelected(): boolean {
            return pageIsSelected(currentSelection(this), this.movies.items)
        },
        selectionCount(): number {
            return countSelection(currentSelection(this), this.movies.totalCount)
        },
        isSelected() {
            return (id: number) => isItemSelected(currentSelection(this), id)
        },
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
            if (filterVal.searchQuery !== this.filter.searchQuery) {
                this.clearSelection()
            }
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
            await this.fetchIncludeSummary()
        },
        async fetchIncludeSummary() {
            this.includeSummary = await services.media.includeSummary('Movie')
        },
        async include(type: MediaType, id: number, include: boolean) {
            await services.media.include(type, id, include)
            await this.fetchIncludeSummary()
        },
        async includeAll(type: MediaType, include: boolean) {
            await services.media.includeAll(type, include)
            await this.fetch()
        },
        async updateThreshold(type: MediaType, id: number, hours: string) {
            await services.media.threshold(type, id, hours)
        },
        applySelection(next: SelectionState<IMovie>) {
            this.selectionMode = next.mode
            this.selectedMovies = next.selected
            this.excludeIds = next.excludeIds
        },
        clearSelection() {
            this.applySelection(emptySelection())
        },
        toggleSelectAll() {
            this.applySelection(togglePage(currentSelection(this), this.movies.items))
        },
        selectAllMatching() {
            this.applySelection(matchingSelection())
        },
        toggleSelect(movie: IMovie) {
            this.applySelection(toggleItem(currentSelection(this), movie))
        }
    }
})

if (import.meta.hot) {
    import.meta.hot.accept(acceptHMRUpdate(useMovieStore, import.meta.hot))
}
