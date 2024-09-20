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
                class="absolute -top-5 left-7 z-50 w-36 transform rounded border border-accent bg-secondary px-3 py-1 text-center text-sm shadow-lg">
                {{ tooltip }}
            </div>
        </Transition>
    </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'

const { tooltip = 'Translation started', duration = 2000 } = defineProps<{
    tooltip?: string
    duration?: number
}>()

const isVisible = ref(false)

const showTooltip = () => {
    isVisible.value = true
    setTimeout(() => {
        isVisible.value = false
    }, duration)
}

defineExpose({
    showTooltip
})
</script>
