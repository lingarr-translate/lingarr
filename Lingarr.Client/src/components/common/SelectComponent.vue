<template>
    <div class="relative">
        <label v-if="label" class="mb-1 block text-sm">
            {{ label }}
        </label>
        <div
            ref="excludeClickOutside"
            class="border-accent flex h-12 cursor-pointer items-center justify-between rounded-md border px-4 py-2"
            @click="toggleDropdown">
            <span v-if="!selected" class="text-gray-400">Select items...</span>
            <div v-else class="flex max-h-12 flex-wrap gap-2 overflow-auto">
                <span
                    class="bg-accent flex cursor-pointer items-center rounded-md px-3 py-1 text-sm font-medium">
                    <span class="text-accent-content mr-2">{{ selectedOption(selected) }}</span>
                </span>
            </div>
            <CaretRightIcon
                :class="{ 'rotate-90': isOpen }"
                class="arrow-right h-5 w-5 transition-transform duration-200" />
        </div>
        <ul
            v-show="isOpen"
            ref="clickOutside"
            class="border-accent bg-primary absolute z-10 mt-1 max-h-60 w-full overflow-auto rounded-md border shadow-lg">
            <li v-if="!options?.length" class="p-3">Select a source language first.</li>
            <li
                v-for="(option, index) in options"
                :key="`${option.value}-${index}`"
                class="cursor-pointer px-4 py-2"
                :class="{ 'bg-accent/20': isSelected(option.value) }"
                @click="selectOption(option)">
                {{ option.label }}
            </li>
        </ul>
    </div>
</template>

<script setup lang="ts">
import { Ref, ref, nextTick } from 'vue'
import CaretRightIcon from '@/components/icons/CaretRightIcon.vue'
import useClickOutside from '@/composables/useClickOutside'

export interface ISelectOption {
    value: string
    label: string
}

const {
    label,
    options,
    selected,
    disabled = false
} = defineProps<{
    label?: string
    options: ISelectOption[]
    selected?: string
    disabled?: boolean
}>()

const emit = defineEmits(['update:selected'])

const isOpen: Ref<boolean> = ref(false)
const clickOutside: Ref<HTMLElement | undefined> = ref()
const excludeClickOutside: Ref<HTMLElement | undefined> = ref()

const toggleDropdown = async () => {
    if (disabled) return
    isOpen.value = !isOpen.value
    await nextTick()
}

const selectOption = (option: ISelectOption) => {
    emit('update:selected', option.value)
    isOpen.value = false
}

const selectedOption = (option: string) => {
    return options.find((item) => item.value === option)?.label
}

const isSelected = (option: string) => {
    return selected == option
}

useClickOutside(
    clickOutside,
    () => {
        isOpen.value = false
    },
    excludeClickOutside
)
</script>
