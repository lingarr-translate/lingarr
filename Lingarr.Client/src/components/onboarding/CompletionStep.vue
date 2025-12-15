<template>
    <div class="space-y-6">
        <div>
            <h2 class="mb-4 text-xl font-semibold text-white">Setup complete!</h2>
        </div>

        <div class="rounded-lg border border-gray-700 bg-gray-900 p-6">
            <h3 class="mb-4 text-lg font-semibold text-white">Your Configuration</h3>

            <div class="space-y-3">
                <div class="flex items-center justify-between">
                    <span class="text-sm text-gray-400">Authentication</span>
                    <span
                        class="rounded px-2 py-1 text-xs font-medium"
                        :class="
                            enableAuth === 'true'
                                ? 'bg-green-900/30 text-green-400'
                                : 'bg-gray-800 text-gray-400'
                        ">
                        {{ enableAuth === 'true' ? 'Enabled' : 'Disabled' }}
                    </span>
                </div>

                <div class="flex items-center justify-between">
                    <span class="text-sm text-gray-400">Telemetry</span>
                    <span
                        class="rounded px-2 py-1 text-xs font-medium"
                        :class="
                            enableTelemetry === 'true'
                                ? 'bg-green-900/30 text-green-400'
                                : 'bg-gray-800 text-gray-400'
                        ">
                        {{ enableTelemetry === 'true' ? 'Enabled' : 'Disabled' }}
                    </span>
                </div>
            </div>

            <div v-if="enableAuth === 'true'" class="mt-4 space-y-2 border-t border-gray-700 pt-4">
                <div class="flex items-center text-xs text-gray-500">
                    <ExclamationIcon class="mr-2 h-4 w-4 text-blue-500" />
                    User account will be created when you complete setup
                </div>
                <div class="flex items-center text-xs text-gray-500">
                    <ExclamationIcon class="mr-2 h-4 w-4 text-blue-500" />
                    API key will be generated when you complete setup
                </div>
            </div>
        </div>
    </div>
</template>

<script setup lang="ts">
import ExclamationIcon from '@/components/icons/ExclamationIcon.vue'

interface Props {
    enableAuth: string
    enableTelemetry: string
}

defineProps<Props>()
</script>
