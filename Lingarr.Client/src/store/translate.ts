import { acceptHMRUpdate, defineStore } from 'pinia'
import { ILanguage, ISubtitle, IUseTranslateStore, MediaType } from '@/ts'
import services from '@/services'
import { useTranslationRequestStore } from '@/store/translationRequest'

export const useTranslateStore = defineStore({
    id: 'translate',
    state: (): IUseTranslateStore => ({
        languages: []
    }),
    getters: {
        getLanguages: (state: IUseTranslateStore): ILanguage[] => state.languages
    },
    actions: {
        async translateSubtitle(
            mediaId: number,
            subtitle: ISubtitle,
            source: string,
            target: ILanguage,
            mediaType: MediaType
        ) {
            await services.translate.translateSubtitle<{ jobId: string }>(
                mediaId,
                subtitle,
                source,
                target,
                mediaType
            )
            await useTranslationRequestStore().getActiveCount()
        },
        async setLanguages(): Promise<void> {
            console.log('setting languages')
            this.languages = await services.translate.getLanguages<ILanguage[]>()
        }
    }
})

if (import.meta.hot) {
    import.meta.hot.accept(acceptHMRUpdate(useTranslateStore, import.meta.hot))
}
