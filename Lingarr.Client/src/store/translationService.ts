import { acceptHMRUpdate, defineStore } from 'pinia'
import { ISettings, ITranslationServiceInstance, IUseTranslationServiceStore } from '@/ts'
import services from '@/services'
import { useSettingStore } from '@/store/setting'

export const useTranslationServiceStore = defineStore('translationService', {
    state: (): IUseTranslationServiceStore => ({
        services: [],
        settings: {},
        loaded: false
    }),
    getters: {
        getServices: (state: IUseTranslationServiceStore): ITranslationServiceInstance[] => {
            return state.services
        },
        getPrimary: (state: IUseTranslationServiceStore) => {
            return state.services.at(0)
        },
        getService: (state: IUseTranslationServiceStore) => (id: number) => {
            return state.services.find((service) => service.id === id)
        },
        getSetting: (state: IUseTranslationServiceStore) => (id: number, key: string) => {
            return state.settings[id]?.[key]
        },
        getHasOwnPrompt: (state: IUseTranslationServiceStore) => (id: number, key: string) => {
            return state.settings[id]?.[key] !== undefined
        },
        getPrompt: (state: IUseTranslationServiceStore) => (id: number, key: keyof ISettings) => {
            return state.settings[id]?.[key] ?? (useSettingStore().getSetting(key) as string) ?? ''
        }
    },
    actions: {
        async loadServices(): Promise<void> {
            this.services = await services.translationService.list()
            this.loaded = true
        },
        async loadSettings(id: number): Promise<void> {
            this.settings[id] = await services.translationService.getSettings(id)
        },
        async createService(
            provider: string,
            name?: string
        ): Promise<ITranslationServiceInstance> {
            const created = await services.translationService.create({ provider, name })
            await this.loadServices()
            return created
        },
        async renameService(id: number, name: string): Promise<void> {
            const renamed = await services.translationService.rename(id, { name })
            const index = this.services.findIndex((service) => service.id === id)
            if (index !== -1) {
                this.services[index] = renamed
            }
        },
        async removeService(id: number): Promise<void> {
            await services.translationService.remove(id)
            delete this.settings[id]
            await this.loadServices()
        },
        async reorderServices(ids: number[]): Promise<void> {
            this.services = await services.translationService.reorder({ ids })
        },
        async updateSetting(
            id: number,
            key: string,
            value: string,
            isValid: boolean = true
        ): Promise<void> {
            if (!this.settings[id]) {
                this.settings[id] = {}
            }
            this.settings[id][key] = value
            if (isValid) {
                await services.translationService.setSetting(id, { key, value })
            }
        },
        async removeSetting(id: number, key: string): Promise<void> {
            await services.translationService.removeSetting(id, key)
            delete this.settings[id]?.[key]
        },
        async updatePrompt(
            id: number,
            key: keyof ISettings,
            value: string,
            isValid: boolean
        ): Promise<void> {
            if (this.getHasOwnPrompt(id, key)) {
                await this.updateSetting(id, key, value, isValid)
            } else {
                await useSettingStore().updateSetting(key, value, isValid)
            }
        },
        async setOwnPrompts(
            id: number,
            keys: (keyof ISettings)[],
            enabled: boolean
        ): Promise<void> {
            const settingsStore = useSettingStore()
            await Promise.all(
                keys.map((key) =>
                    enabled
                        ? this.updateSetting(
                              id,
                              key,
                              (settingsStore.getSetting(key) as string) ?? ''
                          )
                        : this.removeSetting(id, key)
                )
            )
        }
    }
})

if (import.meta.hot) {
    import.meta.hot.accept(acceptHMRUpdate(useTranslationServiceStore, import.meta.hot))
}
