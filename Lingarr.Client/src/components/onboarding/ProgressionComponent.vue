<template>
    <div class="mb-8 flex items-center justify-center space-x-2 md:space-x-4">
        <div v-for="step in totalSteps" :key="step" class="flex items-center">
            <div v-if="step > 1" class="h-0.5 w-8 bg-gray-700 md:w-12"></div>
            <div class="flex items-center">
                <div
                    class="flex h-10 w-10 items-center justify-center rounded-full"
                    :class="
                        currentStep >= step ? 'bg-primary text-white' : 'bg-gray-700 text-gray-400'
                    ">
                    {{ step }}
                </div>
                <span v-if="steps[step - 1]" class="ml-2 hidden text-sm text-gray-400 sm:inline">
                    {{ steps[step - 1] }}
                </span>
            </div>
        </div>
    </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'

const { currentStep, totalSteps, enableAuth } = defineProps<{
    currentStep: number
    totalSteps: number
    enableAuth: string
}>()

const steps = computed(() => {
    const labels = ['Choose']

    if (enableAuth === 'true') {
        labels.push('Account')
    }

    labels.push('Telemetry')
    labels.push('Complete')

    return labels
})
</script>
