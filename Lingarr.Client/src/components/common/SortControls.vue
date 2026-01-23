<template>
    <div class="relative">
        <button
            ref="buttonRef"
            class="border-accent bg-primary text-primary-content focus:border-accent focus:ring-accent flex items-center justify-between gap-2 rounded-md border px-3 py-1.5 text-sm hover:bg-primary/90"
            @click="toggleMenu">
            {{ currentOption?.label }}
            <svg
                class="h-4 w-4"
                fill="none"
                stroke="currentColor"
                viewBox="0 0 24 24">
                <path
                    stroke-linecap="round"
                    stroke-linejoin="round"
                    stroke-width="2"
                    d="M19 9l-7 7-7-7" />
            </svg>
        </button>

        <div
            v-if="isOpen"
            ref="menuRef"
            class="border-accent bg-primary absolute right-0 z-50 mt-1 min-w-[180px] rounded-md border shadow-lg">
            <button
                v-for="(option, index) in options"
                :key="`${option.value}-${index}`"
                class="text-primary-content hover:bg-primary/80 flex w-full items-center justify-between px-4 py-2.5 text-left text-sm first:rounded-t-md last:rounded-b-md"
                :class="{ 'bg-primary/70': modelValue.sortBy === option.value }"
                @click="selectOption(option.value)">
                <span>{{ option.label }}</span>
                <svg
                    v-if="modelValue.sortBy === option.value"
                    class="h-3.5 w-3.5"
                    fill="currentColor"
                    viewBox="0 0 320 512">
                    <path
                        v-if="modelValue.isAscending"
                        d="M182.6 41.4c-12.5-12.5-32.8-12.5-45.3 0l-128 128c-9.2 9.2-11.9 22.9-6.9 34.9s16.6 19.8 29.6 19.8l256 0c12.9 0 24.6-7.8 29.6-19.8s2.2-25.7-6.9-34.9l-128-128z" />
                    <path
                        v-else
                        d="M137.4 470.6c12.5 12.5 32.8 12.5 45.3 0l128-128c9.2-9.2 11.9-22.9 6.9-34.9s-16.6-19.8-29.6-19.8L32 288c-12.9 0-24.6 7.8-29.6 19.8s-2.2 25.7 6.9 34.9l128 128z" />
                </svg>
            </button>
        </div>
    </div>
</template>

<script lang="ts" setup>
import { ref, computed, onMounted, onUnmounted } from 'vue'
import { IOptions, IFilter } from '@/ts'

const emit = defineEmits(['update:modelValue'])
const { modelValue, options } = defineProps<{
    modelValue: IFilter
    options: IOptions[]
}>()

const isOpen = ref(false)
const buttonRef = ref<HTMLElement>()
const menuRef = ref<HTMLElement>()

const currentOption = computed(() => {
    return options.find(opt => opt.value === modelValue.sortBy)
})

const toggleMenu = () => {
    isOpen.value = !isOpen.value
}

const selectOption = (value: string) => {
    if (value === modelValue.sortBy) {
        // Clicking same option - toggle direction
        emit('update:modelValue', {
            ...modelValue,
            isAscending: !modelValue.isAscending
        })
    } else {
        // Clicking different option - change sort field
        emit('update:modelValue', {
            ...modelValue,
            sortBy: value
        })
    }
    isOpen.value = false
}

const handleClickOutside = (event: MouseEvent) => {
    if (
        isOpen.value &&
        buttonRef.value &&
        menuRef.value &&
        !buttonRef.value.contains(event.target as Node) &&
        !menuRef.value.contains(event.target as Node)
    ) {
        isOpen.value = false
    }
}

onMounted(() => {
    document.addEventListener('click', handleClickOutside)
})

onUnmounted(() => {
    document.removeEventListener('click', handleClickOutside)
})
</script>
