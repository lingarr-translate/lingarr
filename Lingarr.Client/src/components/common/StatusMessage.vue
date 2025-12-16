<template>
    <div
        v-if="message"
        class="rounded-md border p-4"
        :class="[borderClass, bgClass]">
        <div class="flex items-start">
            <div v-if="showIcon" class="shrink-0">
                <CheckMarkCircleIcon v-if="type === 'success'" class="h-5 w-5" :class="iconClass" />
                <TimesCircleIcon v-else-if="type === 'error'" class="h-5 w-5" :class="iconClass" />
                <ExclamationIcon v-else class="h-5 w-5" :class="iconClass" />
            </div>
            <div :class="{ 'ml-3': showIcon }">
                <p class="text-sm" :class="textClass">{{ message }}</p>
            </div>
        </div>
    </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import CheckMarkCircleIcon from '@/components/icons/CheckMarkCircleIcon.vue'
import TimesCircleIcon from '@/components/icons/TimesCircleIcon.vue'
import ExclamationIcon from '@/components/icons/ExclamationIcon.vue'

type MessageType = 'success' | 'error' | 'warning' | 'info'

const props = withDefaults(
    defineProps<{
        message?: string
        type?: MessageType
        showIcon?: boolean
    }>(),
    {
        type: 'info',
        showIcon: false
    }
)

const borderClass = computed(() => {
    switch (props.type) {
        case 'success':
            return 'border-green-700/50'
        case 'error':
            return 'border-red-700/50'
        case 'warning':
            return 'border-yellow-700/50'
        case 'info':
            return 'border-blue-700/50'
        default:
            return 'border-gray-700/50'
    }
})

const bgClass = computed(() => {
    switch (props.type) {
        case 'success':
            return 'bg-green-900/20'
        case 'error':
            return 'bg-red-900/20'
        case 'warning':
            return 'bg-yellow-900/20'
        case 'info':
            return 'bg-blue-900/20'
        default:
            return 'bg-gray-900/20'
    }
})

const textClass = computed(() => {
    switch (props.type) {
        case 'success':
            return 'text-green-400'
        case 'error':
            return 'text-red-400'
        case 'warning':
            return 'text-yellow-400'
        case 'info':
            return 'text-blue-400'
        default:
            return 'text-gray-400'
    }
})

const iconClass = computed(() => {
    switch (props.type) {
        case 'success':
            return 'text-green-400'
        case 'error':
            return 'text-red-400'
        case 'warning':
            return 'text-yellow-400'
        case 'info':
            return 'text-blue-400'
        default:
            return 'text-gray-400'
    }
})
</script>
