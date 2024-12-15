import { acceptHMRUpdate, defineStore } from 'pinia'
import { IUseScheduleStore, IRunningJob, ISubtitle } from '@/ts'
import { useLocalStorage } from '@/composables/useLocalStorage'
import services from '@/services'

const localStorage = useLocalStorage()

export const useScheduleStore = defineStore({
    id: 'schedule',
    state: (): IUseScheduleStore => ({
        runningJobs: []
    }),
    getters: {
        getRunningJobs: (state: IUseScheduleStore): IRunningJob[] => state.runningJobs
    },
    actions: {
        setRunningJob(jobId: string, subtitle: ISubtitle): void {
            this.runningJobs = [...this.runningJobs, { jobId, ...subtitle }]
            this.persistRunningJobs()
        },
        async disconnectJob(jobId: string): Promise<void> {
            this.runningJobs = this.runningJobs.filter((job) => job.jobId !== jobId)
            this.persistRunningJobs()
        },
        persistRunningJobs(): void {
            localStorage.setItem('runningJobs', this.runningJobs)
        },
        loadRunningJobs(): void {
            const storedJobs = localStorage.getItem<IRunningJob[]>('runningJobs')
            if (storedJobs) {
                this.runningJobs = storedJobs
            }
        },
        async removeRunningJob(jobId: string): Promise<void> {
            this.runningJobs = this.runningJobs.filter((job) => job.jobId !== jobId)
            await services.schedule.remove(jobId)
            this.persistRunningJobs()
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
