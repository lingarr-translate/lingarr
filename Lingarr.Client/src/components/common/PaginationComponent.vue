<template>
    <nav
        aria-label="Pagination"
        class="flex flex-col items-center justify-between gap-3 px-4 py-6 sm:flex-row sm:px-6">
        <p class="text-secondary-content text-sm" aria-live="polite">
            Page
            <span class="text-primary-content font-semibold">{{ modelValue.pageNumber }}</span>
            of {{ numberOfPages }}
            <span class="mx-1" aria-hidden="true">·</span>
            {{ totalCount }} items
        </p>
        <div class="flex max-w-full items-center gap-1">
            <button
                type="button"
                class="ui-icon-button ui-border border"
                aria-label="Previous page"
                :disabled="modelValue.pageNumber <= 1"
                @click="previous">
                ‹
            </button>
            <template v-for="(page, index) in pages" :key="index">
                <span v-if="page === '...'" class="hidden px-1 sm:inline" aria-hidden="true">
                    …
                </span>
                <button
                    v-else
                    type="button"
                    class="ui-icon-button ui-border hidden border text-sm sm:inline-flex"
                    :class="{
                        'bg-accent/20 border-accent! font-semibold': modelValue.pageNumber === page
                    }"
                    :aria-label="`Page ${page}`"
                    :aria-current="modelValue.pageNumber === page ? 'page' : undefined"
                    @click="setCurrentPage(page)">
                    {{ page }}
                </button>
            </template>
            <span class="px-4 text-sm font-semibold sm:hidden">
                {{ modelValue.pageNumber }} / {{ numberOfPages }}
            </span>
            <button
                type="button"
                class="ui-icon-button ui-border border"
                aria-label="Next page"
                :disabled="modelValue.pageNumber >= numberOfPages"
                @click="next">
                ›
            </button>
        </div>
    </nav>
</template>

<script setup lang="ts">
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

const numberOfPages: ComputedRef<number> = computed(() =>
    pageSize > 0 ? Math.max(1, Math.ceil(totalCount / pageSize)) : 1
)

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
