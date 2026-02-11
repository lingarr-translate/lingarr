<template>
    <CardComponent title="Changelog" description="">
        <template #content>
            <ul class="space-y-8">
                <li
                    v-for="(release, index) in changelog"
                    :key="release.date"
                    class="group relative pl-6"
                    :class="{ 'pb-8': index !== changelog.length - 1 }">
                    <div class="absolute left-0 top-0 h-full">
                        <div class="absolute left-0 top-0 -ml-px h-full w-[2px] bg-accent" />
                        <div
                            class="absolute left-0 top-0 -ml-2 h-4 w-4 rounded-full border-2 border-accent bg-primary" />
                    </div>

                    <div class="space-y-3">
                        <div class="flex flex-wrap items-center gap-3">
                            <time
                                :datetime="release.date"
                                class="flex items-center text-sm font-medium">
                                <svg
                                    class="mr-1.5 h-4 w-4"
                                    xmlns="http://www.w3.org/2000/svg"
                                    viewBox="0 0 24 24"
                                    fill="none"
                                    stroke="currentColor"
                                    stroke-width="2"
                                    stroke-linecap="round"
                                    stroke-linejoin="round">
                                    <rect x="3" y="4" width="18" height="18" rx="2" ry="2" />
                                    <line x1="16" y1="2" x2="16" y2="6" />
                                    <line x1="8" y1="2" x2="8" y2="6" />
                                    <line x1="3" y1="10" x2="21" y2="10" />
                                </svg>
                                {{ formatDate(release.date) }}
                            </time>
                            <span
                                v-if="release.version"
                                class="inline-flex items-center rounded-full border border-accent bg-primary px-2.5 py-0.5 text-xs font-medium">
                                v{{ release.version }}
                            </span>
                        </div>

                        <h3 class="text-lg font-semibold">
                            {{ release.title }}
                        </h3>

                        <ul class="space-y-2">
                            <li
                                v-for="(change, changeIndex) in release.changes"
                                :key="changeIndex"
                                class="flex items-start">
                                <span class="mr-2 mt-2 h-1 w-1 shrink-0 rounded-full bg-accent" />
                                <span v-highlight class="flex-1">{{ change }}</span>
                            </li>
                        </ul>
                    </div>
                </li>
            </ul>
        </template>
    </CardComponent>
</template>

<script setup>
import { ref } from 'vue'
import { formatDate } from '@/utils/date.ts'
import CardComponent from '@/components/common/CardComponent.vue'

const changelog = ref([
    {
        date: '2025',
        version: '0.9.5',
        title: '',
        changes: ['']
    },
    {
        date: '2024-10-30',
        version: '0.9.4',
        title: 'Enhanced Notifications and Translation History',
        changes: [
            'Changed volume mapping and removed `/app/media` which is a `BREAKING CHANGE`  make sure you map the new volumes and synchronise your libraries via the settings.',
            'Added a new notification system which displays automated and manually instantiated translations.',
            '... read more on GitHub'
        ]
    }
])
</script>
