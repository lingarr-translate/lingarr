import { acceptHMRUpdate, defineStore } from 'pinia'
import { IOnboardingStore } from '@/ts'

export const useOnboardingStore = defineStore('onboarding', {
    state: (): IOnboardingStore => ({
        enableAuth: 'false',
        enableTelemetry: 'false',
        username: '',
        password: '',
        confirmPassword: '',
        currentStep: 1
    }),
    getters: {
        getEnableAuth: (state): string => state.enableAuth
    },
    actions: {
        setEnableAuth(value: string) {
            this.enableAuth = value
        },
        setEnableTelemetry(value: string) {
            this.enableTelemetry = value
        },
        setCurrentStep(value: number) {
            this.currentStep = value
        },
        resetOnboarding() {
            this.enableAuth = 'false'
            this.enableTelemetry = 'false'
            this.username = ''
            this.password = ''
            this.confirmPassword = ''
            this.currentStep = 1
        }
    }
})

if (import.meta.hot) {
    import.meta.hot.accept(acceptHMRUpdate(useOnboardingStore, import.meta.hot))
}
