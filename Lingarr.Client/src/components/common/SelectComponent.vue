<template>
    <div class="relative min-w-0">
        <label v-if="label" :for="id || selectId" class="mb-2 block text-sm font-medium">
            {{ label }}
        </label>
        <div class="relative">
            <select
                :id="id || selectId"
                :value="selected"
                :disabled="disabled"
                :aria-label="label || placeholder"
                :aria-busy="isLoading"
                class="ui-input w-full cursor-pointer px-3 py-2 disabled:opacity-50"
                :class="size === 'sm' ? 'text-sm' : ''"
                @focus="loadOptions"
                @pointerdown="loadOptions"
                @keydown.enter="loadOptions"
                @keydown.space="loadOptions"
                @change="selectOption">
                <option value="" disabled>
                    {{ isLoading ? 'Loading options…' : placeholder }}
                </option>
                <option
                    v-if="selected && !options.some((option) => option.value === selected)"
                    :value="selected">
                    {{ selected }}
                </option>
                <option v-if="!sortedOptions.length" disabled>{{ noOptions }}</option>
                <option v-for="option in sortedOptions" :key="option.value" :value="option.value">
                    {{ option.label }}
                </option>
            </select>
        </div>
    </div>
</template>

<script setup lang="ts">
import { ref, computed, useId } from 'vue'
import { ISelectOption } from '@/ts'

const props = withDefaults(
    defineProps<{
        id?: string
        label?: string
        options: ISelectOption[]
        selected?: string
        disabled?: boolean
        loadOnOpen?: boolean
        placeholder?: string
        noOptions?: string
        size?: 'sm' | 'md'
    }>(),
    {
        label: '',
        options: () => [],
        selected: '',
        placeholder: 'Select items...',
        noOptions: 'Select a source language first.',
        size: 'md'
    }
)
const emit = defineEmits(['update:selected', 'fetch-options'])
const selectId = useId()
const isLoading = ref(false)
const sortedOptions = computed(() =>
    [...props.options].sort((a, b) => a.label.localeCompare(b.label))
)
const loadOptions = () => {
    if (!props.disabled && props.loadOnOpen && !isLoading.value) {
        isLoading.value = true
        emit('fetch-options')
    }
}
const selectOption = (event: Event) =>
    emit('update:selected', (event.target as HTMLSelectElement).value)
const setLoadingState = (loading: boolean) => {
    isLoading.value = loading
}
defineExpose({ setLoadingState })
</script>
