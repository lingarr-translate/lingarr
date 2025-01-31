<template>
    <div ref="clickOutside" class="relative z-10 inline-block text-left">
        <div
            class="inline-flex h-10 w-10 cursor-pointer items-center justify-center"
            @click="toggleDropdown">
            <slot name="button"></slot>
        </div>

        <transition
            enter-active-class="transition ease-out duration-100"
            enter-from-class="transform opacity-0 scale-95"
            enter-to-class="transform opacity-100 scale-100"
            leave-active-class="transition ease-in duration-75"
            leave-from-class="transform opacity-100 scale-100"
            leave-to-class="transform opacity-0 scale-95">
            <div
                v-if="isOpen"
                :class="getClasses()"
                class="border-accent bg-secondary absolute right-0 mt-2 origin-top-right rounded-md border shadow-lg">
                <slot name="content"></slot>
            </div>
        </transition>
    </div>
</template>

<script setup lang="ts">
import { ref, Ref } from 'vue'
import useClickOutside from '@/composables/useClickOutside'

const isOpen = ref(false)
const clickOutside: Ref<HTMLElement | undefined> = ref()
const { width = 'medium' } = defineProps<{
    width: string
}>()

const getClasses = () => {
    switch (width) {
        case 'medium':
            return 'w-48'
        case 'large':
            return 'w-80 md:w-96'
        default:
            return 'w-20'
    }
}

const toggleDropdown = () => {
    isOpen.value = !isOpen.value
}

useClickOutside(clickOutside, () => {
    isOpen.value = false
})
</script>
