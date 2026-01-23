import { acceptHMRUpdate, defineStore } from 'pinia'
import services from '@/services'
import { IFilter, IUseShowStore, IPagedResult, IShow, MediaType, MEDIA_TYPE } from '@/ts'

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
        selectAll: false
    }),
    getters: {
        getFilter: (state: IUseShowStore): IFilter => state.filter,
        get: (state: IUseShowStore): IPagedResult<IShow> => state.shows,
        // Reactive computed summary from existing show data on current page
        // Reactive computed summary from API response with global counts
        includeSummary(state: IUseShowStore) {
            const total = state.shows.totalCount || 0
            const included = state.shows.includedCount ?? 0
            const excluded = state.shows.excludedCount ?? 0
            
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
            this.shows = await services.media.shows(
                this.filter.pageNumber,
                this.filter.searchQuery,
                this.filter.sortBy,
                this.filter.isAscending
            )
        },
        async include(type: MediaType, id: number, include: boolean) {
            try {
                await services.media.include(type, id, include)
                // Update local state with cascading after successful API call
                const show = this.shows.items?.find(s => s.id === id)
                if (show && type === MEDIA_TYPE.SHOW) {
                    show.excludeFromTranslation = !include
                    // Cascade to seasons and episodes client-side
                    show.seasons?.forEach(season => {
                        season.excludeFromTranslation = !include
                        season.episodes?.forEach(episode => {
                            episode.excludeFromTranslation = !include
                        })
                    })
                }
                // Handle season-level includes
                if (type === MEDIA_TYPE.SEASON) {
                    for (const show of this.shows.items || []) {
                        const season = show.seasons?.find(s => s.id === id)
                        if (season) {
                            season.excludeFromTranslation = !include
                            // Cascade to episodes
                            season.episodes?.forEach(episode => {
                                episode.excludeFromTranslation = !include
                            })
                            break
                        }
                    }
                }
                // Handle episode-level includes
                if (type === MEDIA_TYPE.EPISODE) {
                    for (const show of this.shows.items || []) {
                        for (const season of show.seasons || []) {
                            const episode = season.episodes?.find(e => e.id === id)
                            if (episode) {
                                episode.excludeFromTranslation = !include
                                return
                            }
                        }
                    }
                }
            } catch (error) {
                console.error('Failed to update include state', error)
                throw error
            }
        },
        async includeAll(include: boolean) {
            try {
                await services.media.includeAll(MEDIA_TYPE.SHOW, include)
                // Update all local show states with cascading after successful API call
                this.shows.items?.forEach(show => {
                    show.excludeFromTranslation = !include
                    show.seasons?.forEach(season => {
                        season.excludeFromTranslation = !include
                        season.episodes?.forEach(episode => {
                            episode.excludeFromTranslation = !include
                        })
                    })
                })
            } catch (error) {
                console.error('Failed to update all shows include state', error)
                throw error
            }
        },
        async updateThreshold(type: MediaType, id: number, hours: string) {
            await services.media.threshold(type, id, hours)
        },
        clearSelection() {
            this.selectedShows = []
            this.selectAll = false
        },
        toggleSelectAll() {
            if (this.selectAll) {
                this.selectedShows = []
                this.selectAll = false
            } else {
                this.selectedShows = [...this.shows.items]
                this.selectAll = true
            }
        },
        toggleSelect(show: IShow) {
            const index = this.selectedShows.findIndex((s) => s.id === show.id)
            if (index === -1) {
                this.selectedShows.push(show)
            } else {
                this.selectedShows.splice(index, 1)
            }
            this.selectAll = this.selectedShows.length === this.shows.items.length
        }
    }
})

if (import.meta.hot) {
    import.meta.hot.accept(acceptHMRUpdate(useShowStore, import.meta.hot))
}
