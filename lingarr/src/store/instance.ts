import { acceptHMRUpdate, defineStore } from 'pinia'
import { IUseInstanceStore, IStatus, STATUS } from '@/ts/instance'

export const useInstanceStore = defineStore({
    id: 'instance',
    state: (): IUseInstanceStore => ({
        loading: false,
        status: STATUS.NONE
    }),
    getters: {
        getLoading: (state): boolean => state.loading,
        getStatus: (state): IStatus => state.status
    },
    actions: {
        setLoading(status: boolean): void {
            this.loading = status
        },
        setStatus(status: IStatus): void {
            this.status = status
            setTimeout(() => {
                this.status = STATUS.NONE
            }, 4000)
        }
    }
})

if (import.meta.hot) {
    import.meta.hot.accept(acceptHMRUpdate(useInstanceStore, import.meta.hot))
}
