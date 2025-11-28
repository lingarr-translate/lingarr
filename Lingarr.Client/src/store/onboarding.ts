import { acceptHMRUpdate, defineStore } from 'pinia'

interface IOnboardingStore {
    enableUserAuth: string
    enableApiKey: string
    username: string
    password: string
    confirmPassword: string
    generatedApiKey: string
    currentStep: number
}

export const useOnboardingStore = defineStore({
    id: 'onboarding',
    state: (): IOnboardingStore => ({
        enableUserAuth: 'false',
        enableApiKey: 'false',
        username: '',
        password: '',
        confirmPassword: '',
        generatedApiKey: '',
        currentStep: 1
    }),
    getters: {
        getEnableUserAuth: (state): string => state.enableUserAuth,
        getEnableApiKey: (state): string => state.enableApiKey
    },
    actions: {
        setEnableUserAuth(value: string) {
            this.enableUserAuth = value
        },
        setEnableApiKey(value: string) {
            this.enableApiKey = value
        },
        setGeneratedApiKey(value: string) {
            this.generatedApiKey = value
        },
        setCurrentStep(value: number) {
            this.currentStep = value
        },
        resetOnboarding() {
            this.enableUserAuth = 'false'
            this.enableApiKey = 'false'
            this.username = ''
            this.password = ''
            this.confirmPassword = ''
            this.generatedApiKey = ''
            this.currentStep = 1
        }
    }
})

if (import.meta.hot) {
    import.meta.hot.accept(acceptHMRUpdate(useOnboardingStore, import.meta.hot))
}
