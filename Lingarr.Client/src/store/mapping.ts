import { acceptHMRUpdate, defineStore } from 'pinia'
import services from '@/services'
import { IPathMapping } from '@/ts'

export const useMappingStore = defineStore({
    id: 'mapping',
    state: () => ({
        mappings: [] as IPathMapping[],
        error: null as string | null
    }),
    actions: {
        async fetchMappings() {
            this.error = null
            try {
                this.mappings = await services.mapping.getMappings()
            } catch (error) {
                this.error = 'Failed to fetch mappings'
                console.error('Error fetching mappings:', error)
            }
        },
        async setMappings(newMappings: IPathMapping[]) {
            this.error = null
            try {
                await services.mapping.setMappings(newMappings)
                this.mappings = newMappings
            } catch (error) {
                this.error = 'Failed to set mappings'
                console.error('Error setting mappings:', error)
            }
        },
        addMapping(mapping: IPathMapping) {
            this.mappings.push(mapping)
        },
        removeMapping(index: number) {
            this.mappings.splice(index, 1)
        },
        updateMapping(index: number, updatedMapping: IPathMapping) {
            this.mappings[index] = updatedMapping
        }
    },
    getters: {
        getMappingByIndex: (state) => (index: number) => state.mappings[index],
        getMappingsByMediaType: (state) => (mediaType: string) =>
            state.mappings.filter((mapping) => mapping.mediaType === mediaType)
    }
})

if (import.meta.hot) {
    import.meta.hot.accept(acceptHMRUpdate(useMappingStore, import.meta.hot))
}
