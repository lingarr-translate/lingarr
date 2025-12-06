import { acceptHMRUpdate, defineStore } from 'pinia'
import { IUseScheduleStore, IRecurringJob } from '@/ts'
import services from '@/services'

export const useScheduleStore = defineStore('schedule', {
    state: (): IUseScheduleStore => ({
        recurringJobs: []
    }),
    getters: {
        getRecurringJobs: (state: IUseScheduleStore): IRecurringJob[] => state.recurringJobs
    },
    actions: {
        async fetchRecurringJobs(): Promise<void> {
            this.recurringJobs = await services.schedule.recurringJobs()
        },
        async startJob(jobName: string): Promise<void> {
            await services.schedule.startJob(jobName)
        },
        async indexShows(): Promise<void> {
            await services.schedule.indexShows()
        },
        async indexMovies(): Promise<void> {
            await services.schedule.indexMovies()
        }
    }
})

if (import.meta.hot) {
    import.meta.hot.accept(acceptHMRUpdate(useScheduleStore, import.meta.hot))
}
