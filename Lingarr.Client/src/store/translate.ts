import { acceptHMRUpdate, defineStore } from 'pinia'
import { ILanguage, ISubtitle, MediaType } from '@/ts'
import services from '@/services'
import { useTranslationRequestStore } from '@/store/translationRequest'

export const useTranslateStore = defineStore({
    id: 'translate',
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
        }
    }
})

if (import.meta.hot) {
    import.meta.hot.accept(acceptHMRUpdate(useTranslateStore, import.meta.hot))
}
