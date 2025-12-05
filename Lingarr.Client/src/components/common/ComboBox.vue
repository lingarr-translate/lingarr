<template>
    <div class="relative">
        <label v-if="label" class="mb-1 block text-sm">
            {{ label }}
        </label>
        <div class="relative">
            <input
                ref="inputRef"
                v-model="searchQuery"
                type="text"
                :placeholder="placeholder"
                :disabled="disabled"
                class="border-accent w-full cursor-text rounded-md border bg-transparent px-4 py-2 pr-10 outline-none transition-colors focus:border-blue-500"
                @focus="openDropdown"
                @input="handleInput" />
            
            <div class="absolute right-0 top-0 flex h-full items-center px-3">
                <LoaderCircleIcon v-if="isLoading" class="mr-2 h-4 w-4 animate-spin text-gray-400" />
                <CaretRightIcon
                    class="h-5 w-5 text-gray-400 transition-transform duration-200"
                    :class="{ 'rotate-90': isOpen }"
                    @click="toggleDropdown" />
            </div>
        </div>

        <ul
            v-show="isOpen"
            ref="dropdownRef"
            class="border-accent bg-primary absolute z-50 mt-1 max-h-60 w-full overflow-auto rounded-md border shadow-lg">
            <li v-if="!filteredOptions.length" class="p-3 text-gray-400 text-sm">
                {{ noOptions || 'No results found' }}
            </li>
            <li
                v-for="(option, index) in filteredOptions"
                :key="`${option.value}-${index}`"
                class="cursor-pointer px-4 py-2 hover:bg-gray-100 dark:hover:bg-gray-700"
                :class="{ 'bg-blue-50 dark:bg-blue-900/20': isSelected(option.value) }"
                @click="selectOption(option)">
                {{ option.label }}
            </li>
        </ul>
    </div>
</template>

<script setup lang="ts">
import { ref, computed, watch, onMounted, onBeforeUnmount } from 'vue'
import CaretRightIcon from '@/components/icons/CaretRightIcon.vue'
import LoaderCircleIcon from '@/components/icons/LoaderCircleIcon.vue'

export interface ISelectOption {
    value: string
    label: string
}

const props = withDefaults(
    defineProps<{
        label?: string
        options: ISelectOption[]
        selected?: string // Model value (the ID/key)
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
        noOptions: 'No options available'
    }
)

const emit = defineEmits(['update:selected', 'fetch-options'])

const isOpen = ref(false)
const isLoading = ref(false)
const searchQuery = ref('')
const inputRef = ref<HTMLInputElement | null>(null)
const dropdownRef = ref<HTMLElement | null>(null)

// Initialize search query with the label of the selected option
const initializeSearchQuery = () => {
    const selectedOption = props.options.find(o => o.value === props.selected)
    if (selectedOption) {
        searchQuery.value = selectedOption.label
    }
}

watch(() => props.selected, () => {
    initializeSearchQuery()
})

watch(() => props.options, () => {
    initializeSearchQuery()
})

const filteredOptions = computed(() => {
    if (!searchQuery.value) {
        return props.options
    }
    const query = searchQuery.value.toLowerCase()
    
    // If exact match exists, show all (assume user just selected something)
    // Actually, logic: if the query matches exactly the selected item's label, show all
    const selectedOption = props.options.find(o => o.value === props.selected)
    if (selectedOption && searchQuery.value === selectedOption.label) {
        return props.options
    }

    return props.options.filter(option => 
        option.label.toLowerCase().includes(query)
    )
})

const isSelected = (value: string) => {
    return props.selected === value
}

const openDropdown = () => {
    if (props.disabled) return
    isOpen.value = true
    if (props.loadOnOpen) {
        isLoading.value = true
        emit('fetch-options')
        // Mock loading finish or expect parent to handle? 
        // SelectComponent logic didn't unset isLoading, assuming parent or just purely trigger.
        // Let's mimic SelectComponent behavior but safer.
        setTimeout(() => isLoading.value = false, 1000) // Safety timeout
    }
}

const toggleDropdown = () => {
    if (isOpen.value) {
        closeDropdown()
    } else {
        openDropdown()
        inputRef.value?.focus()
    }
}

const closeDropdown = () => {
    isOpen.value = false
    // Reset query to selected value on close if no new selection made
    initializeSearchQuery()
}

const handleInput = () => {
    if (!isOpen.value) {
        isOpen.value = true
    }
}

const selectOption = (option: ISelectOption) => {
    emit('update:selected', option.value)
    searchQuery.value = option.label
    isOpen.value = false
}

// Click outside handler
const handleClickOutside = (event: MouseEvent) => {
    if (
        dropdownRef.value && 
        inputRef.value &&
        !dropdownRef.value.contains(event.target as Node) &&
        !inputRef.value.contains(event.target as Node)
    ) {
        closeDropdown()
    }
}

onMounted(() => {
    document.addEventListener('click', handleClickOutside)
    initializeSearchQuery()
})

onBeforeUnmount(() => {
    document.removeEventListener('click', handleClickOutside)
})

// Expose setLoading for parent to control
const setLoadingState = (loading: boolean) => {
    isLoading.value = loading
}

defineExpose({
    setLoadingState
})
</script>
