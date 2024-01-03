<template>
    <PageLayout>
        <div
            v-for="(item, visibleResourcesIndex) in paginatedResources"
            :key="'item' + visibleResourcesIndex"
            class="w-full">
            <div class="flex flex-row items-center text-base">
                <div class="flex basis-8/12 items-center">
                    <FileIcon class="mr-3 h-4 w-3 fill-current text-neutral-400" />
                    <span class="overflow-x-auto whitespace-nowrap">
                        {{ item.name }}
                    </span>
                </div>
                <div class="flex basis-3/12 space-x-2">
                    <LanguageBadge
                        v-for="(subtitle, languageBadgeIndex) in item.subtitles"
                        :key="`subtitle-${languageBadgeIndex}`"
                        :subtitle="subtitle" />
                </div>
                <div class="flex basis-1/12 justify-end">
                    <ContextMenu :item="item" />
                </div>
            </div>
        </div>

        <PaginationNavigation
            :entries="entries"
            :current-page="currentPage"
            :total-pages="totalPages"
            :visible-page-numbers="visiblePageNumbers"
            @previous="previousPage"
            @next="nextPage"
            @go-to-page="goToPage" />
    </PageLayout>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useResourceStore } from '@/store/resource'
import FileIcon from '@/components/icons/FileIcon.vue'
import ContextMenu from '@/components/ContextMenu.vue'
import LanguageBadge from '@/components/LanguageBadge.vue'
import PageLayout from '@/components/layout/PageLayout.vue'
import PaginationNavigation from '@/components/PaginationNavigation.vue'

const maxVisiblePages = ref(6)
const itemsPerPage = ref(10)
const currentPage = ref(1)
const resourceStore = useResourceStore()

const nextPage = () => {
    if (currentPage.value < totalPages.value) {
        currentPage.value++
    }
}

const previousPage = () => {
    if (currentPage.value > 1) {
        currentPage.value--
    }
}

const goToPage = (pageNumber: number) => {
    currentPage.value = pageNumber
}

const resources = computed(() => resourceStore.getResource)
// Paginated resources
const paginatedResources = computed(() => resources.value.slice(startIndex.value, endIndex.value))

// Calculate total number of pages
const totalPages = computed(() => Math.ceil(resources.value.length / itemsPerPage.value))
const entries = computed(() => resources.value.length)

// Calculate the index range for the current page
const startIndex = computed(() => (currentPage.value - 1) * itemsPerPage.value)
const endIndex = computed(() => currentPage.value * itemsPerPage.value)

// Calculate visible page numbers with a maximum of 3
const visiblePageNumbers = computed(() => {
    const startPage = Math.max(1, currentPage.value - Math.floor(maxVisiblePages.value / 2))
    const endPage = Math.min(totalPages.value, startPage + maxVisiblePages.value - 1)

    const pages = []
    for (let i = startPage; i <= endPage; i++) {
        pages.push(i)
    }

    if (startPage > 1) {
        pages.unshift('...')
    }
    if (endPage < totalPages.value) {
        pages.push('...')
    }

    return pages
})

onMounted(() => {
    resourceStore.setResource()
})
</script>
