<template>
    <div class="space-y-6">
        <div>
            <h2 class="mb-4 text-xl font-semibold text-white">Choose your authentication method</h2>
            <p class="mb-6 text-sm text-gray-400">
                Select how you want to secure access to Lingarr. You can enable both methods or skip
                to use Lingarr without authentication.
            </p>
        </div>

        <div
            class="rounded-lg border-2 p-6 transition-colors"
            :class="
                enableUserAuth == 'true'
                    ? 'border-gray-700 bg-gray-900'
                    : 'border-primary bg-primary/10'
            ">
            <div class="flex items-start">
                <div class="ml-4 flex-1">
                    <h3 class="text-lg font-semibold text-white">User Authentication</h3>
                    <p class="mt-1 text-sm text-gray-400">
                        Create a username and password to secure the web interface. Recommended for
                        most users.
                    </p>
                </div>
                <ToggleButton v-model="enableUserAuth" />
            </div>
        </div>

        <div
            class="rounded-lg border-2 p-6 transition-colors"
            :class="
                enableApiKey == 'true'
                    ? 'border-gray-700 bg-gray-900'
                    : 'border-primary bg-primary/10'
            ">
            <div class="flex items-start">
                <div class="ml-4 flex-1">
                    <h3 class="text-lg font-semibold text-white">API Key</h3>
                    <p class="mt-1 text-sm text-gray-400">
                        Generate an API key for programmatic access. Useful for scripts and external
                        integrations.
                    </p>
                </div>
                <ToggleButton v-model="enableApiKey" />
            </div>
        </div>
    </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { useOnboardingStore } from '@/store/onboarding'
import ToggleButton from '@/components/common/ToggleButton.vue'

const onboardingStore = useOnboardingStore()

const enableUserAuth = computed({
    get: (): string => onboardingStore.getEnableUserAuth,
    set: (newValue: string): void => {
        onboardingStore.setEnableUserAuth(newValue)
    }
})

const enableApiKey = computed({
    get: (): string => onboardingStore.getEnableApiKey,
    set: (newValue: string): void => {
        onboardingStore.setEnableApiKey(newValue)
    }
})
</script>
