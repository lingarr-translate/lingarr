<template>
    <div class="relative">
        <label v-if="label" class="mb-1 block text-sm">
            {{ label }}
        </label>
        <div
            ref="excludeClickOutside"
            class="flex h-12 cursor-pointer items-center justify-between rounded-md border border-accent px-4 py-2"
            @click="toggleDropdown">
            <span v-if="!selected" class="text-gray-400">{{ placeholder }}</span>
            <div v-else class="flex max-h-12 flex-wrap gap-2 overflow-auto">
                <span
                    class="flex cursor-pointer items-center rounded-md bg-accent px-3 py-1 text-sm font-medium">
                    <span class="mr-2 text-accent-content">{{ displaySelectedLabel }}</span>
                </span>
            </div>
            <div class="flex items-center">
                <LoaderCircleIcon v-if="isLoading" class="mr-2 h-4 w-4 animate-spin" />
                <CaretRightIcon
                    :class="{ 'rotate-90': isOpen }"
                    class="arrow-right h-5 w-5 transition-transform duration-200" />
            </div>
        </div>
        <ul
            v-show="isOpen"
            ref="clickOutside"
            class="absolute z-10 mt-1 max-h-60 w-full overflow-auto rounded-md border border-accent bg-primary shadow-lg">
            <li v-if="!sortedOptions.length" class="p-3">{{ noOptions }}</li>
            <li
                v-for="(option, index) in sortedOptions"
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
import { Ref, ref, nextTick, computed } from 'vue'
import CaretRightIcon from '@/components/icons/CaretRightIcon.vue'
import LoaderCircleIcon from '@/components/icons/LoaderCircleIcon.vue'
import useClickOutside from '@/composables/useClickOutside'

export interface ISelectOption {
    value: string
    label: string
}

const props = withDefaults(
    defineProps<{
        label?: string
        options: ISelectOption[]
        selected?: string
        disabled?: boolean
        loadOnOpen?: boolean
        placeholder?: string
        noOptions?: string
    }>(),
    {
        label: '',
        options: () => [],
        selected: '',
        placeholder: 'Select items...',
        noOptions: 'Select a source language first.',
        selectedLabel: ''
    }
)

const emit = defineEmits(['update:selected', 'fetch-options'])

const isOpen: Ref<boolean> = ref(false)
const isLoading: Ref<boolean> = ref(false)
const clickOutside: Ref<HTMLElement | undefined> = ref()
const excludeClickOutside: Ref<HTMLElement | undefined> = ref()

const sortedOptions = computed(() => {
    return [...props.options].sort((a, b) => a.label.localeCompare(b.label))
})

const displaySelectedLabel = computed(() => {
    const option = props.options.find((item) => item.value === props.selected)
    return option ? option.label : props.selected
})

const toggleDropdown = async () => {
    if (props.disabled) return

    isOpen.value = !isOpen.value

    if (isOpen.value && props.loadOnOpen) {
        isLoading.value = true
        emit('fetch-options')
    }

    await nextTick()
}

const setLoadingState = async (loading: boolean) => {
    isLoading.value = loading
}

const selectOption = (option: ISelectOption) => {
    emit('update:selected', option.value)
    isOpen.value = false
}

const isSelected = (option: string) => {
    return props.selected == option
}

useClickOutside(
    clickOutside,
    () => {
        isOpen.value = false
    },
    excludeClickOutside
)

defineExpose({
    setLoadingState
})
</script>
