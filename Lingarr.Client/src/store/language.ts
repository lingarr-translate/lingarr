import { acceptHMRUpdate, defineStore } from 'pinia'
import { useLocalStorage } from '@/composables/useLocalStorage'
import { ILanguage, ILanguageState } from '@/ts/language'
import services from '@/services'

export const useLanguageStore = defineStore({
    id: 'directory',
    state: (): ILanguageState => ({
        languages: [],
        usedLanguages: []
    }),
    getters: {
        getLanguages(): ILanguage[] {
            return this.languages
        },
        getUsedLanguages(): ILanguage[] {
            const storage = useLocalStorage()
            const usedLanguages = storage.getItem<ILanguage[] | null>('mostUsedLanguages')
            // Sort most used values by count in descending order
            return usedLanguages?.sort((a, b) => b.count - a.count).slice(0, 3) ?? []
        }
    },
    actions: {
        async setLanguages() {
            this.languages = await services.language.list<ILanguage[]>()
        },
        addUsedLanguages(language: ILanguage) {
            const storage = useLocalStorage()
            const usedLanguages = this.getUsedLanguages
            // Add the value to the most used values list
            const existingValueIndex = usedLanguages.findIndex(
                (item) => item.code === language.code
            )
            if (existingValueIndex !== -1) {
                usedLanguages[existingValueIndex].count += 1
            } else {
                language.count++
                usedLanguages.push(language)
            }
            storage.setItem('mostUsedLanguages', usedLanguages)
        }
    }
})

if (import.meta.hot) {
    import.meta.hot.accept(acceptHMRUpdate(useLanguageStore, import.meta.hot))
}
