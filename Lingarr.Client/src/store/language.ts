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
        getUsedLanguages(state): ILanguage[] {
            const storage = useLocalStorage()
            state.usedLanguages = storage.getItem<ILanguage[] | null>('mostUsedLanguages')
            return state.usedLanguages?.sort((a, b) => b.count - a.count).slice(0, 3) ?? []
        }
    },
    actions: {
        async setLanguages() {
            this.languages = await services.language.list<ILanguage[]>()
        },
        addUsedLanguage(language: ILanguage) {
            const storage = useLocalStorage()
            const usedLanguages = this.getUsedLanguages
            // Add the value to the most used values list
            const existingValueIndex = usedLanguages.findIndex(
                (item: ILanguage) => item.code === language.code
            )
            if (existingValueIndex !== -1) {
                usedLanguages[existingValueIndex].count += 1
            } else {
                language.count++
                usedLanguages.push(language)
            }
            storage.setItem('mostUsedLanguages', usedLanguages)
        },
        removeUsedLanguage(language: ILanguage) {
            const storage = useLocalStorage()
            const usedLanguages = this.getUsedLanguages
            // Find the index of the language to remove
            const languageIndexToRemove = usedLanguages.findIndex(
                (item: ILanguage) => item.code === language.code
            )
            if (languageIndexToRemove !== -1) {
                usedLanguages.splice(languageIndexToRemove, 1)
                this.usedLanguages = [...usedLanguages]
                storage.setItem('mostUsedLanguages', usedLanguages)
            }
        }
    }
})

if (import.meta.hot) {
    import.meta.hot.accept(acceptHMRUpdate(useLanguageStore, import.meta.hot))
}
