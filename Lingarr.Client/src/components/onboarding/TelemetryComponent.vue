<template>
    <div class="space-y-6">
        <div>
            <h2 class="mb-4 text-xl font-semibold text-white">Help Improve Lingarr</h2>
            <p class="mb-6 text-sm text-gray-400">
                Share anonymous usage statistics to help us improve Lingarr.
            </p>
        </div>

        <StatusMessage
            type="success"
            message="No personal information, file names, or subtitle content is ever collected." />

        <div class="flex flex-col space-y-4">
            <span class="text-sm">Example of telemetry data:</span>
            <div class="rounded-md bg-primary p-4">
                <pre class="overflow-x-auto rounded-md p-4 text-xs">{{
                    JSON.stringify(previewData, null, 2)
                }}</pre>
            </div>
        </div>
    </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import StatusMessage from '@/components/common/StatusMessage.vue'

const previewData = ref({
    version: '1.0.3',
    reportDate: '2025-12-15',
    platform: 'linux/x64',
    metrics: {
        filesTranslated: 5,
        linesTranslated: 3773,
        charactersTranslated: 77225,
        serviceUsage: {
            localai: 5
        },
        languagePairs: {
            'en:nl': 5
        },
        mediaTypeUsage: {
            Episode: 5
        },
        modelUsage: {
            'localai:openai/gpt-4o': 5
        }
    }
})
</script>
