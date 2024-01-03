<template>
    <nav class="pagination mt-4 flex w-full items-center justify-center md:justify-between">
        <span class="hidden text-neutral-500 md:block">
            {{ currentPage }} to {{ totalPages }} of {{ entries }} entries
        </span>
        <div class="space-x-2">
            <button
                :disabled="currentPage === 1"
                class="rounded border px-3 py-1"
                :class="
                    currentPage === 1
                        ? 'text-neutral-400 dark:border-neutral-700 dark:text-neutral-500'
                        : 'border-neutral-600 dark:border-neutral-300'
                "
                @click="emit('previous')">
                &#9665;
            </button>
            <template v-for="pageNumber in visiblePageNumbers">
                <button
                    v-if="pageNumber !== '...'"
                    :key="`shown-${pageNumber}`"
                    :disabled="pageNumber === currentPage"
                    :class="
                        pageNumber === currentPage
                            ? 'text-neutral-400 dark:border-neutral-700 dark:text-neutral-500'
                            : 'border-neutral-600 dark:border-neutral-300'
                    "
                    class="rounded border px-3 py-1"
                    @click="emit('goToPage', pageNumber)">
                    {{ pageNumber }}
                </button>
                <span v-else :key="`hidden-${pageNumber}`" class="px-3 py-1">...</span>
            </template>
            <button
                :disabled="currentPage === totalPages"
                class="rounded border px-3 py-1"
                :class="
                    currentPage === totalPages
                        ? 'text-neutral-400 dark:border-neutral-700 dark:text-neutral-500'
                        : 'border-neutral-600 dark:border-neutral-300'
                "
                @click="emit('next')">
                &#9655;
            </button>
        </div>
    </nav>
</template>

<script setup lang="ts">
const emit = defineEmits(['goToPage', 'previous', 'next'])

interface Props {
    currentPage: number
    totalPages: number
    entries: number
    visiblePageNumbers: (string | number)[]
}
defineProps<Props>()
</script>
