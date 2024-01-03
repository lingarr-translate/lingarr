import { acceptHMRUpdate, defineStore } from 'pinia'
import { useInstanceStore } from '@/store/instance'
import { useLanguageStore } from '@/store/language'
import { ILanguage } from '@/ts/language'
import { STATUS } from '@/ts/instance'
import services from '@/services'

export const useTranslateStore = defineStore({
    id: 'translate',
    actions: {
        async translateSubtitles(path: string, language: ILanguage) {
            useInstanceStore().setLoading(true)
            useLanguageStore().addUsedLanguages(language)

            services.translate
                .translate(path, language.code)
                .then(async () => {
                    await useInstanceStore().setLoading(false)
                    await useInstanceStore().setStatus(STATUS.SUCCESS)
                })
                .catch(async () => {
                    useInstanceStore().setLoading(false)
                    await useInstanceStore().setStatus(STATUS.ERROR)
                })
        }
    }
})

if (import.meta.hot) {
    import.meta.hot.accept(acceptHMRUpdate(useTranslateStore, import.meta.hot))
}
