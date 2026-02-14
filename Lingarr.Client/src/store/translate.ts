import { acceptHMRUpdate, defineStore } from 'pinia'
import { ILanguage, ISubtitle, IUseTranslateStore, MediaType } from '@/ts'
import services from '@/services'
import useTranslationRequestStore from '@/store/translationRequest'

export const useTranslateStore = defineStore('translate', {
    state: (): IUseTranslateStore => ({
        languages: [],
        languagesError: false,
        languagesLoading: false
    }),
    getters: {
        getLanguages: (state: IUseTranslateStore): ILanguage[] => state.languages,
        hasLanguagesError: (state: IUseTranslateStore): boolean => state.languagesError,
        isLanguagesLoading: (state: IUseTranslateStore): boolean => state.languagesLoading
    },
    actions: {
        async translateSubtitle(
            mediaId: number,
            subtitle: ISubtitle,
            source: string,
            target: ILanguage,
            mediaType: MediaType
        ): Promise<number> {
            const result = await services.translate.translateSubtitle<{ jobId: number }>(
                mediaId,
                subtitle,
                source,
                target,
                mediaType
            )
            await useTranslationRequestStore().getActiveCount()
            return result.jobId
        },
        async bulkTranslate(
            mediaIds: number[],
            targetLanguage: string,
            mediaType: MediaType
        ): Promise<void> {
            await services.translate.bulkTranslate<number[]>(
                mediaIds,
                targetLanguage,
                mediaType
            )
            await useTranslationRequestStore().getActiveCount()
        },
        async setLanguages(): Promise<void> {
            try {
                this.languagesLoading = true
                this.languagesError = false
                this.languages = await services.translate.getLanguages<ILanguage[]>()
            } catch (error) {
                console.error('Failed to load languages:', error)
                this.languagesError = true
                this.languages = []
            } finally {
                this.languagesLoading = false
            }
        }
    }
})

if (import.meta.hot) {
    import.meta.hot.accept(acceptHMRUpdate(useTranslateStore, import.meta.hot))
}
