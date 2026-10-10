<template>
    <div class="relative inline-block">
        <slot></slot>
        <Transition
            enter-active-class="transition ease-out duration-300"
            enter-from-class="transform opacity-0 scale-95"
            enter-to-class="transform opacity-100 scale-100"
            leave-active-class="transition ease-in duration-200"
            leave-from-class="transform opacity-100 scale-100"
            leave-to-class="transform opacity-0 scale-95">
            <div
                v-if="isVisible"
                role="status"
                :class="alignmentClass"
                class="border-accent bg-secondary absolute bottom-full z-50 mb-2 w-36 transform rounded-sm border px-3 py-1 text-center text-sm shadow-lg">
                {{ tooltip }}
            </div>
        </Transition>
    </div>
</template>

<script setup lang="ts">
import { ref, computed, ComputedRef, onUnmounted } from 'vue'

const {
    tooltip = 'Translation started',
    duration = 2000,
    alignment = 'left'
} = defineProps<{
    tooltip?: string
    duration?: number
    alignment?: string
}>()

const isVisible = ref(false)
const alignmentClass: ComputedRef<string> = computed(() => {
    switch (alignment) {
        case 'right':
            return 'right-7'
        case 'left':
            return 'left-7'
        default:
            return 'left-1/2 -translate-x-1/2'
    }
})

let timer: ReturnType<typeof setTimeout> | undefined
onUnmounted(() => clearTimeout(timer))
const showTooltip = () => {
    clearTimeout(timer)
    isVisible.value = true
    timer = setTimeout(() => {
        isVisible.value = false
    }, duration)
}

defineExpose({
    showTooltip
})
</script>
