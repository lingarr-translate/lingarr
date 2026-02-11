<template>
    <transition
        enter-active-class="transition ease-out duration-300 transform"
        enter-from-class="translate-y-2 opacity-0 sm:translate-y-0 sm:translate-x-2"
        enter-to-class="translate-y-0 opacity-100 sm:translate-x-0"
        leave-active-class="transition ease-in duration-200"
        leave-from-class="opacity-100"
        leave-to-class="opacity-0">
        <div
            v-if="isVisible"
            class="pointer-events-auto w-full max-w-sm overflow-hidden rounded-lg shadow-lg ring-1 ring-black ring-opacity-5">
            <div class="p-4">
                <div class="flex items-start">
                    <div class="shrink-0">
                        <CheckMarkCircleIcon
                            v-if="type === 'success'"
                            class="h-6 w-6 text-green-400" />
                        <TimesCircleIcon
                            v-else-if="type === 'error'"
                            class="h-6 w-6 text-red-400" />
                        <ExclamationIcon v-else class="h-6 w-6 text-blue-400" />
                    </div>
                    <div class="ml-3 w-0 flex-1 pt-0.5">
                        <p class="text-sm font-medium text-gray-900">
                            {{ title }}
                        </p>
                        <p class="mt-1 text-sm text-gray-500">
                            {{ message }}
                        </p>
                    </div>
                    <div class="ml-4 flex shrink-0">
                        <button class="inline-flex rounded-md bg-white" @click="close">
                            <span class="sr-only">Close</span>
                            <TimesIcon class="h-5 w-5" />
                        </button>
                    </div>
                </div>
            </div>
            <div
                class="h-1 transition-all duration-300 ease-out"
                :class="{
                    'bg-green-500': type === 'success',
                    'bg-red-500': type === 'error',
                    'bg-blue-500': type === 'info'
                }"
                :style="{ width: `${progress}%` }"></div>
        </div>
    </transition>
</template>

<script setup lang="ts">
import { ref, onMounted, onUnmounted } from 'vue'
import CheckMarkCircleIcon from '@/components/icons/CheckMarkCircleIcon.vue'
import TimesCircleIcon from '@/components/icons/TimesCircleIcon.vue'
import ExclamationIcon from '@/components/icons/ExclamationIcon.vue'
import TimesIcon from '@/components/icons/TimesIcon.vue'

type Status = 'success' | 'error' | 'info'

const {
    title,
    message,
    type,
    duration = 5000
} = defineProps<{
    title: string
    message: string
    type: Status
    duration?: number
}>()

const emit = defineEmits(['close'])

const isVisible = ref(true)
const progress = ref(100)

let timer: ReturnType<typeof setTimeout> | undefined
let progressTimer: number | undefined

function close() {
    isVisible.value = false
    emit('close')
}

onMounted(() => {
    timer = setTimeout(close, duration)

    const startTime = Date.now()
    const updateProgress = () => {
        const elapsedTime = Date.now() - startTime
        progress.value = 100 - (elapsedTime / duration) * 100
        if (progress.value > 0) {
            progressTimer = requestAnimationFrame(updateProgress)
        }
    }
    progressTimer = requestAnimationFrame(updateProgress)
})

onUnmounted(() => {
    if (timer) {
        clearTimeout(timer)
    }
    if (progressTimer) {
        cancelAnimationFrame(progressTimer)
    }
})
</script>
