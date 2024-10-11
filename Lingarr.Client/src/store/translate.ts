import { acceptHMRUpdate, defineStore } from 'pinia'
import { ILanguage, ISubtitle } from '@/ts'
import { useScheduleStore } from '@/store/schedule'
import { useSignalR } from '@/composables/useSignalR'
import services from '@/services'

const signalR = useSignalR()

export const useTranslateStore = defineStore({
    id: 'translate',
    actions: {
        async translateSubtitle(subtitle: ISubtitle, source: string, target: ILanguage) {
            const scheduleStore = useScheduleStore()
            const { jobId } = await services.translate.translateSubtitle<{ jobId: string }>(
                subtitle,
                source,
                target
            )
            scheduleStore.setRunningJob(jobId, subtitle)
            const scheduleProgress = await signalR.connect(
                'ScheduleProgress',
                '/signalr/ScheduleProgress'
            )
            await scheduleProgress.joinGroup({ group: jobId })
        }
    }
})

if (import.meta.hot) {
    import.meta.hot.accept(acceptHMRUpdate(useTranslateStore, import.meta.hot))
}
