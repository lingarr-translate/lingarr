import { acceptHMRUpdate, defineStore } from 'pinia'
import { ILanguage, ISubtitle } from '@/ts'
import { useScheduleStore } from '@/store/schedule'
import services from '@/services'

export const useTranslateStore = defineStore({
    id: 'translate',
    actions: {
        async translateSubtitle(subtitle: ISubtitle, language: ILanguage) {
            const scheduleStore = useScheduleStore()
            const { jobId } = await services.translate.translateSubtitle<{ jobId: string }>(
                subtitle,
                language
            )
            scheduleStore.setRunningJob(jobId, subtitle)
        }
    }
})

if (import.meta.hot) {
    import.meta.hot.accept(acceptHMRUpdate(useTranslateStore, import.meta.hot))
}
