import { acceptHMRUpdate, defineStore } from 'pinia'
import { IResource, IResourceState } from '@/ts/resource'
import services from '@/services'

export const useResourceStore = defineStore({
    id: 'resource',
    state: (): IResourceState => ({
        filter: '',
        mediaType: 'tvshows',
        resource: []
    }),
    getters: {
        getFilter: (state): string => state.filter,
        getMediaType: (state): string => state.mediaType,
        getResource: (state): IResource[] => state.resource.search('name', state.filter)
    },
    actions: {
        setFilter(search: string) {
            this.filter = search
        },
        setMediaType(type: string) {
            this.mediaType = type
        },
        async setResource() {
            this.resource = await services.resource.list<IResource[]>(this.getMediaType)
        }
    }
})

if (import.meta.hot) {
    import.meta.hot.accept(acceptHMRUpdate(useResourceStore, import.meta.hot))
}
