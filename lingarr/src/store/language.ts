import { acceptHMRUpdate, defineStore } from 'pinia'
import { useInstanceStore } from '@/store/instance'
import services from '@/services'
import { STATUS } from '@/ts/instance'

interface ILanguage {
    count: number
    code: string
}

interface ILanguageState {
    languages: ILanguage[]
    usedLanguages: ILanguage[]
}

export const useLanguageStore = defineStore({
    id: 'directory',
    state: (): ILanguageState => ({
        languages: [],
        usedLanguages: []
    }),
    getters: {
        getUsedLanguages() {
            const storedValues = localStorage.getItem('mostUsedLanguages')
            const usedLanguages: ILanguage[] = storedValues ? JSON.parse(storedValues) : []
            // Sort most used values by count in descending order
            return usedLanguages.sort((a, b) => b.count - a.count)
        },
        getUniqueLanguages() {
            return this.languages.filter((language) => {
                return !this.getUsedLanguages.some((selectedLanguage) => {
                    return selectedLanguage.code === language.code
                })
            })
        },
        getEnrichedUsedLanguages() {
            // filter and sort languages
            return this.languages
                .filter((language) =>
                    this.getUsedLanguages.some((selectedLanguage) => {
                        return selectedLanguage.code === language.code
                    })
                )
                .sort((a, b) => {
                    const countA = this.getUsedLanguages.find(
                        (selectedLanguage) => selectedLanguage.code === a.code
                    ).count
                    const countB = this.getUsedLanguages.find(
                        (selectedLanguage) => selectedLanguage.code === b.code
                    ).count
                    return countB - countA
                })
        }
    },
    actions: {
        async setLanguages() {
            this.languages = await services.language.list()
        },
        addUsedLanguages(code) {
            const usedLanguages = this.getUsedLanguages
            // Add the value to the most used values list
            const existingValueIndex = usedLanguages.findIndex((item) => item.code === code)
            if (existingValueIndex !== -1) {
                usedLanguages[existingValueIndex].count += 1
            } else {
                usedLanguages.push({ code, count: 1 })
            }
            localStorage.setItem('mostUsedLanguages', JSON.stringify(usedLanguages))
        },
        async translateSubtitles(path, targetLanguage, callback) {
            const language = targetLanguage ?? null
            if (!path || language == null) return
            if (path.isSrt()) {
                useInstanceStore().setLoading(true)
                useLanguageStore().addUsedLanguages(targetLanguage)

                services.translate
                    .translate(path, language)
                    .then(async () => {
                        await useInstanceStore().setLoading(false)
                        await useInstanceStore().setStatus(STATUS.SUCCESS)
                        callback()
                    })
                    .catch(async () => {
                        useInstanceStore().setLoading(false)
                        await useInstanceStore().setStatus(STATUS.ERROR)
                    })
            }
        }
    }
})

if (import.meta.hot) {
    import.meta.hot.accept(acceptHMRUpdate(useLanguageStore, import.meta.hot))
}
