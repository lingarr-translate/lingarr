import { acceptHMRUpdate, defineStore } from 'pinia'

interface IOnboardingStore {
    enableAuth: string
    username: string
    password: string
    confirmPassword: string
    currentStep: number
}

export const useOnboardingStore = defineStore('onboarding',{
    state: (): IOnboardingStore => ({
        enableAuth: 'false',
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
        setCurrentStep(value: number) {
            this.currentStep = value
        },
        resetOnboarding() {
            this.enableAuth = 'false'
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
