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
        get: (state: IUseShowStore): IPagedResult<IShow> => state.shows,
        includeSummary(): IIncludeSummary {
            const totalShows = this.shows.items?.length || 0
            const excludedShows = this.shows.items?.filter(s => s.excludeFromTranslation).length || 0
            return {
                totalMovies: 0,
                excludedMovies: 0,
                totalShows,
                excludedShows
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
            await services.media.include(type, id, include)
            
            // Update local state with cascading
            if (type === MEDIA_TYPE.SHOW) {
                const show = this.shows.items?.find(s => s.id === id)
                if (show) {
                    show.excludeFromTranslation = !include
                    // Cascade to seasons and episodes
                    show.seasons?.forEach(season => {
                        season.excludeFromTranslation = !include
                        season.episodes?.forEach(episode => {
                            episode.excludeFromTranslation = !include
                        })
                    })
                }
            } else if (type === MEDIA_TYPE.SEASON) {
                // Find the season in any show
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
            } else if (type === MEDIA_TYPE.EPISODE) {
                // Find the episode in any show/season
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
        },
        async includeAll(include: boolean) {
            await services.media.includeAll(MEDIA_TYPE.SHOW, include)
            // Update all local state with cascading
            this.shows.items?.forEach(show => {
                show.excludeFromTranslation = !include
                show.seasons?.forEach(season => {
                    season.excludeFromTranslation = !include
                    season.episodes?.forEach(episode => {
                        episode.excludeFromTranslation = !include
                    })
                })
            })
            await this.fetch()
        },
        async updateThreshold(type: MediaType, id: number, hours: string) {
            await services.media.threshold(type, id, hours)
        }
    }
})

if (import.meta.hot) {
    import.meta.hot.accept(acceptHMRUpdate(useShowStore, import.meta.hot))
}
