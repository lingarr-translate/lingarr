<template>
    <div class="flex min-h-screen items-center justify-center px-4">
        <div class="w-full max-w-3xl">
            <CardComponent title="Welcome to Lingarr">
                <template #description>Let's set up your authentication preferences</template>
                <template #content>
                    <div
                        v-if="error"
                        class="mb-6 rounded-md border border-red-700/50 bg-red-900/20 p-4">
                        <p class="text-sm text-red-400">{{ error }}</p>
                    </div>

                    <ProgressionComponent
                        :current-step="onboardingStore.currentStep"
                        :total-steps="totalSteps"
                        :enable-user-auth="onboardingStore.enableUserAuth"
                        :enable-api-key="onboardingStore.enableApiKey" />

                    <div data-info="onboarding-steps">
                        <ChooseAuthentication v-if="stepType === 'choose'" />

                        <AccountCreation
                            v-if="stepType === 'user'"
                            v-model:username="onboardingStore.username"
                            v-model:password="onboardingStore.password"
                            v-model:confirm-password="onboardingStore.confirmPassword"
                            @update:validation="handleValidationUpdate" />

                        <ApiKeyConfiguration
                            v-if="stepType === 'api'"
                            :api-key="onboardingStore.generatedApiKey || undefined"
                            @api-key-generated="handleApiKeyGenerated" />

                        <CompletionStep
                            v-if="stepType === 'final'"
                            :enable-user-auth="onboardingStore.enableUserAuth"
                            :enable-api-key="onboardingStore.enableApiKey" />
                    </div>

                    <div
                        class="mt-8 flex items-center"
                        :class="stepType === 'choose' ? 'justify-end' : 'justify-between'">
                        <ButtonComponent
                            v-if="onboardingStore.currentStep > 1"
                            variant="primary"
                            :disabled="loading"
                            @click="goToPreviousStep">
                            Back
                        </ButtonComponent>

                        <ButtonComponent
                            v-if="onboardingStore.currentStep < lastStep"
                            variant="accent"
                            :disabled="loading || !canProceed"
                            :loading="loading"
                            @click="goToNextStep">
                            <span v-if="stepType === 'choose'">Continue</span>
                            <span v-else>Continue</span>
                        </ButtonComponent>

                        <ButtonComponent
                            v-if="stepType === 'final'"
                            variant="accent"
                            :loading="loading"
                            :disabled="loading"
                            @click="completeSetup">
                            {{ loading ? 'Completing...' : 'Go to Dashboard' }}
                        </ButtonComponent>
                    </div>
                </template>
            </CardComponent>
        </div>
    </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import { useRouter } from 'vue-router'
import { useOnboardingStore } from '@/store/onboarding'
import { useSettingStore } from '@/store/setting'
import services from '@/services'
import CardComponent from '@/components/common/CardComponent.vue'
import ButtonComponent from '@/components/common/ButtonComponent.vue'
import ProgressionComponent from '@/components/onboarding/ProgressionComponent.vue'
import ChooseAuthentication from '@/components/onboarding/ChooseAuthentication.vue'
import AccountCreation from '@/components/onboarding/AccountCreation.vue'
import ApiKeyConfiguration from '@/components/onboarding/ApiKeyConfiguration.vue'
import CompletionStep from '@/components/onboarding/CompletionStep.vue'

const router = useRouter()
const onboardingStore = useOnboardingStore()
const settingStore = useSettingStore()

const loading = ref(false)
const error = ref('')

const isValid = ref({
    username: false,
    password: false,
    confirmPassword: false
})

// Steps
const stepOrder = computed(() => {
    const steps = ['choose']
    if (onboardingStore.enableUserAuth === 'true') {
        steps.push('user')
    }
    if (onboardingStore.enableApiKey === 'true') {
        steps.push('api')
    }
    steps.push('final')
    return steps
})

const totalSteps = computed(() => stepOrder.value.length)
const lastStep = computed(() => totalSteps.value)
const stepType = computed(() => stepOrder.value[onboardingStore.currentStep - 1])

const canProceed = computed(() => {
    if (stepType.value === 'user') {
        return (
            isValid.value.username &&
            isValid.value.password &&
            isValid.value.confirmPassword &&
            onboardingStore.password === onboardingStore.confirmPassword &&
            onboardingStore.password.length >= 4
        )
    }
    return true
})

const handleValidationUpdate = (field: string, valid: boolean) => {
    if (field in isValid.value) {
        isValid.value[field as keyof typeof isValid.value] = valid
    }
}

const handleApiKeyGenerated = (apiKey: string) => {
    onboardingStore.setGeneratedApiKey(apiKey)
}

const goToNextStep = async () => {
    if (!canProceed.value) {
        error.value =
            stepType.value === 'choose'
                ? 'Please select at least one authentication method'
                : 'Please fill in all required fields correctly'
        return
    }

    error.value = ''

    if (stepType.value === 'user') {
        loading.value = true
        try {
            await services.auth.signup({
                username: onboardingStore.username,
                password: onboardingStore.password
            })
        } catch (err: any) {
            console.error('Signup error:', err)
            error.value = err?.data?.message || 'Failed to create account. Please try again.'
            loading.value = false
            return
        } finally {
            loading.value = false
        }
    }

    onboardingStore.setCurrentStep(onboardingStore.currentStep + 1)
}

const goToPreviousStep = () => {
    if (onboardingStore.currentStep > 1) {
        onboardingStore.setCurrentStep(onboardingStore.currentStep - 1)
        error.value = ''
    }
}

const submitOnboarding = async () => {
    loading.value = true
    error.value = ''

    try {
        await services.auth.completeOnboarding({
            enableUserAuth: onboardingStore.enableUserAuth,
            enableApiKey: onboardingStore.enableApiKey
        })
    } catch (err: any) {
        console.error('Onboarding error:', err)
        error.value = err?.data?.message || 'An error occurred during setup. Please try again.'
    } finally {
        loading.value = false
    }
}

const completeSetup = async () => {
    await submitOnboarding()
    if (!error.value) {
        onboardingStore.resetOnboarding()
        await settingStore.applySettingsOnLoad()
        router.push('/')
    }
}
</script>
