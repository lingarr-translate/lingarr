<template>
    <div class="relative w-full md:w-96">
        <input
            :value="modelValue.searchQuery"
            type="text"
            placeholder="Search media..."
            class="block w-full rounded-md border border-accent bg-primary px-8 py-1 text-sm text-primary-content placeholder-primary/60 outline-none"
            @input="search" />
        <SearchIcon
            class="absolute left-2 top-1/2 h-4 w-4 -translate-y-1/2 transform text-accent-content" />
        <TimesIcon
            v-if="modelValue.searchQuery"
            class="absolute right-2 top-1/2 h-4 w-4 -translate-y-1/2 transform cursor-pointer text-accent-content"
            @click="clear" />
    </div>
</template>

<script lang="ts" setup>
import { IFilter } from '@/ts'
import SearchIcon from '@/components/icons/SearchIcon.vue'
import TimesIcon from '@/components/icons/TimesIcon.vue'

const emit = defineEmits(['update:modelValue'])
const { modelValue } = defineProps<{
    modelValue: IFilter
}>()

const searchQuery = (value: string) => {
    emit('update:modelValue', {
        ...modelValue,
        searchQuery: value
    })
}

const search = (event: Event) => {
    searchQuery((event.target as HTMLInputElement).value)
}

const clear = () => {
    searchQuery('')
}
</script>
