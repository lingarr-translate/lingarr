import { acceptHMRUpdate, defineStore } from 'pinia'
import services from '@/services'
import { IFilter, IUseShowStore, IPagedResult, IShow, ISeason, MediaType } from '@/ts'

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
        },
        selectedShows: [],
        selectedSeasons: {},
        selectAll: false
    }),
    getters: {
        getFilter: (state: IUseShowStore): IFilter => state.filter,
        get: (state: IUseShowStore): IPagedResult<IShow> => state.shows,
        selectedEpisodeCount: (state: IUseShowStore): number => {
            return Object.values(state.selectedSeasons).reduce(
                (count, season) => count + season.episodes.length,
                0
            )
        }
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
        async exclude(type: MediaType, id: number) {
            await services.media.exclude(type, id)
        },
        async updateThreshold(type: MediaType, id: number, hours: string) {
            await services.media.threshold(type, id, hours)
        },
        clearSelection() {
            this.selectedShows = []
            this.selectedSeasons = {}
            this.selectAll = false
        },
        toggleSelectAll() {
            if (this.selectAll) {
                this.selectedShows = []
                this.selectedSeasons = {}
                this.selectAll = false
            } else {
                this.selectedShows = [...this.shows.items]
                this.selectedSeasons = {}
                for (const show of this.shows.items) {
                    for (const season of show.seasons) {
                        this.selectedSeasons[season.id] = season
                    }
                }
                this.selectAll = true
            }
        },
        toggleSelect(show: IShow) {
            const index = this.selectedShows.findIndex((s) => s.id === show.id)
            if (index === -1) {
                this.selectedShows.push(show)
                for (const season of show.seasons) {
                    this.selectedSeasons[season.id] = season
                }
            } else {
                this.selectedShows.splice(index, 1)
                for (const season of show.seasons) {
                    delete this.selectedSeasons[season.id]
                }
            }
            this.selectAll = this.selectedShows.length === this.shows.items.length
        },
        toggleSeasonSelect(season: ISeason, show: IShow) {
            if (this.selectedSeasons[season.id]) {
                delete this.selectedSeasons[season.id]
                const showIndex = this.selectedShows.findIndex((s) => s.id === show.id)
                if (showIndex !== -1) {
                    this.selectedShows.splice(showIndex, 1)
                }
            } else {
                this.selectedSeasons[season.id] = season
                const allSeasonsSelected = show.seasons.every((s) => this.selectedSeasons[s.id])
                if (allSeasonsSelected) {
                    if (!this.selectedShows.some((s) => s.id === show.id)) {
                        this.selectedShows.push(show)
                    }
                }
            }
            this.selectAll = this.selectedShows.length === this.shows.items.length
        }
    }
})

if (import.meta.hot) {
    import.meta.hot.accept(acceptHMRUpdate(useShowStore, import.meta.hot))
}
