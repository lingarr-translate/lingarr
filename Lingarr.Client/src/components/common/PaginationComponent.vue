<template>
    <div class="flex w-full justify-center p-4 md:items-center md:justify-between">
        <div class="hidden md:block">
            {{ translate('common.page') }}
            <span class="font-bold">{{ modelValue.pageNumber }}</span>
            {{ translate('common.of') }}
            <span class="font-bold">{{ numberOfPages }}</span>
            {{ translate('common.of') }}
            <span class="font-bold">{{ totalCount }}</span>
            {{ translate('common.items') }}
        </div>
        <div>
            <ul v-if="numberOfPages >= 1" class="flex space-x-1 md:space-x-2">
                <li
                    class="text-md flex h-7 w-8 cursor-pointer items-center justify-center border border-accent bg-secondary font-semibold text-primary-content md:h-9 md:w-10 md:text-lg"
                    :class="{
                        disabled: modelValue.pageNumber === 1
                    }"
                    @click="previous">
                    <span>&laquo;</span>
                </li>
                <li
                    v-for="page in pages"
                    :key="page"
                    class="flex h-7 w-8 items-center justify-center border border-accent text-xs font-semibold text-primary-content md:h-9 md:w-10"
                    :class="{
                        'cursor-pointer': page !== '...',
                        'bg-secondary': modelValue.pageNumber === page
                    }"
                    @click="page !== '...' && setCurrentPage(page)">
                    <span>{{ page }}</span>
                </li>
                <li
                    class="text-md flex h-7 w-8 cursor-pointer items-center justify-center border border-accent bg-secondary font-semibold text-primary-content md:h-9 md:w-10 md:text-lg"
                    @click="next">
                    <div>&raquo;</div>
                </li>
            </ul>
        </div>
    </div>
</template>

<script lang="ts" setup>
import { computed, ComputedRef } from 'vue'
import { IFilter } from '@/ts'

const emit = defineEmits(['update:modelValue'])
const {
    modelValue,
    totalCount = 0,
    pageSize = 0
} = defineProps<{
    modelValue: IFilter
    totalCount?: number
    pageSize?: number
}>()

const numberOfPages: ComputedRef<number> = computed(() => Math.ceil(totalCount / pageSize))

const pages: ComputedRef<(string | number)[]> = computed(() => {
    const pages = []
    const totalPages = numberOfPages.value
    const current = modelValue.pageNumber

    if (totalPages <= 7) {
        for (let index = 1; index <= totalPages; index++) {
            pages.push(index)
        }
    } else {
        pages.push(1)
        if (current > 3) {
            pages.push('...')
        }

        let start = Math.max(2, current - 1)
        let end = Math.min(totalPages - 1, current + 1)

        if (current <= 3) {
            end = 5
        }
        if (current >= totalPages - 2) {
            start = totalPages - 4
        }

        for (let index = start; index <= end; index++) {
            pages.push(index)
        }

        if (current < totalPages - 2) {
            pages.push('...')
        }
        pages.push(totalPages)
    }

    return pages
})

const setCurrentPage = (number: string | number) => {
    emit('update:modelValue', {
        ...modelValue,
        pageNumber: number
    })
}

const previous = () => {
    if (modelValue.pageNumber > 1) {
        setCurrentPage(modelValue.pageNumber - 1)
    }
}

const next = () => {
    if (modelValue.pageNumber < numberOfPages.value) {
        setCurrentPage(modelValue.pageNumber + 1)
    }
}
</script>
