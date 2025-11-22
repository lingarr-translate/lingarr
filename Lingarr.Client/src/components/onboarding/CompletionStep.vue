<template>
    <div class="space-y-6">
        <div>
            <h2 class="mb-4 text-xl font-semibold text-white">Setup complete!</h2>
        </div>

        <div class="rounded-lg border border-gray-700 bg-gray-900 p-6">
            <h3 class="mb-4 text-lg font-semibold text-white">Your Configuration</h3>
            <div class="space-y-3">
                <div class="flex items-center justify-between">
                    <span class="text-sm text-gray-400">User Authentication</span>
                    <span
                        class="rounded px-2 py-1 text-xs font-medium"
                        :class="
                            enableUserAuth === 'true'
                                ? 'bg-green-900/30 text-green-400'
                                : 'bg-gray-800 text-gray-400'
                        ">
                        {{ enableUserAuth === 'true' ? 'Enabled' : 'Disabled' }}
                    </span>
                </div>
                <div class="flex items-center justify-between">
                    <span class="text-sm text-gray-400">API Key Authentication</span>
                    <span
                        class="rounded px-2 py-1 text-xs font-medium"
                        :class="
                            enableApiKey === 'true'
                                ? 'bg-green-900/30 text-green-400'
                                : 'bg-gray-800 text-gray-400'
                        ">
                        {{ enableApiKey === 'true' ? 'Enabled' : 'Disabled' }}
                    </span>
                </div>
            </div>
        </div>

        <div class="rounded-md border border-green-700/50 bg-green-900/20 p-4">
            <p class="text-sm text-green-400">
                Your authentication settings have been configured. You can modify these settings
                later in the authentication settings page.
            </p>
        </div>
    </div>
</template>

<script setup lang="ts">
interface Props {
    enableUserAuth: string
    enableApiKey: string
}

defineProps<Props>()
</script>
