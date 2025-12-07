<template>
    <div class="space-y-6">
        <div>
            <h2 class="mb-4 text-xl font-semibold text-white">Secure your Lingarr instance</h2>
            <p class="mb-6 text-sm text-gray-400">
                Enable authentication to protect your Lingarr instance with both session-based login
                and API key access, or continue without authentication.
            </p>
        </div>

        <div
            class="rounded-lg border-2 p-6 transition-colors"
            :class="
                enableAuth == 'true'
                    ? 'border-gray-700 bg-gray-900'
                    : 'border-primary bg-primary/10'
            ">
            <div class="flex items-start">
                <div class="ml-4 flex-1">
                    <h3 class="text-lg font-semibold text-white">Enable Authentication</h3>
                    <p class="mt-1 text-sm text-gray-400">
                        Secure the web interface with username/password login and generate an API
                        key for programmatic access. Recommended for production use.
                    </p>
                    <div class="mt-3 space-y-1">
                        <div class="flex items-center text-xs text-gray-500">
                            <CheckMarkIcon class="mr-2 h-4 w-4 text-green-500" v-if="enableAuth === 'true'" />
                            <TimesIcon class="mr-2 h-4 w-4 text-red-500" v-else />
                            Session-based authentication for web interface
                        </div>
                        <div class="flex items-center text-xs text-gray-500">
                            <CheckMarkIcon class="mr-2 h-4 w-4 text-green-500" v-if="enableAuth === 'true'" />
                            <TimesIcon class="mr-2 h-4 w-4 text-red-500" v-else />
                            API key for scripts and integrations
                        </div>
                    </div>
                </div>
                <ToggleButton v-model="enableAuth" />
            </div>
        </div>

        <div
            class="rounded-md border border-yellow-700/50 bg-yellow-900/20 p-4">
            <p class="text-sm text-yellow-400">
                Without authentication, anyone with access to your network can use Lingarr. This is
                only recommended for private, trusted networks.
            </p>
        </div>
    </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { useOnboardingStore } from '@/store/onboarding'
import ToggleButton from '@/components/common/ToggleButton.vue'
import CheckMarkIcon from '@/components/icons/CheckMarkIcon.vue'
import TimesIcon from '@/components/icons/TimesIcon.vue'

const onboardingStore = useOnboardingStore()

const enableAuth = computed({
    get: (): string => onboardingStore.getEnableAuth,
    set: (newValue: string): void => {
        onboardingStore.setEnableAuth(newValue)
    }
})
</script>
