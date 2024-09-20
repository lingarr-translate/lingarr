<template>
    <CardComponent title="Lingarr Roadmap" description="">
        <template #description>Upcoming changes and improvements</template>
        <template #content>
            <div v-for="(item, index) in roadmapItems" :key="index" class="flex items-start">
                <div
                    class="mr-4 flex h-8 w-8 flex-shrink-0 items-center justify-center rounded-full bg-gray-700">
                    <component
                        :is="item.icon"
                        v-if="item.icon"
                        class="h-4 w-4 text-primary-content" />
                </div>
                <div class="flex-grow">
                    <h3 class="text-lg font-semibold text-primary-content">{{ item.title }}</h3>
                    <p class="text-sm text-secondary-content/50">{{ item.description }}</p>
                    <p class="mt-1 text-xs text-tertiary-content">{{ item.date }}</p>
                </div>
                <div class="ml-4 flex-shrink-0">
                    <span
                        class="rounded-full px-2 py-1 text-xs font-medium"
                        :class="[
                            item.status === 'Completed'
                                ? 'bg-green-500 text-green-100'
                                : item.status === 'In Progress'
                                  ? 'bg-blue-500 text-blue-100'
                                  : 'bg-gray-600 text-gray-100'
                        ]">
                        {{ item.status }}
                    </span>
                </div>
            </div>
        </template>
    </CardComponent>
</template>

<script setup lang="ts">
import { shallowRef } from 'vue'
import AutomationIcon from '@/components/icons/AutomationIcon.vue'
import NotificationIcon from '@/components/icons/NotificationIcon.vue'
import HistoryIcon from '@/components/icons/HistoryIcon.vue'
import LanguageIcon from '@/components/icons/LanguageIcon.vue'
import BuildingIcon from '@/components/icons/BuildingIcon.vue'
import CardComponent from '@/components/common/CardComponent.vue'

const roadmapItems = shallowRef([
    {
        icon: BuildingIcon,
        title: 'Application Rebuild',
        description: 'Rebuilt application from the ground up',
        date: '2024',
        status: 'Completed'
    },
    {
        icon: NotificationIcon,
        title: 'Notifications',
        description: 'Implement a simple notification system for requested translations',
        date: '2024',
        status: 'Completed'
    },
    {
        icon: AutomationIcon,
        title: 'Automated Translation',
        description: 'Automated translation for subtitles',
        date: '2024',
        status: 'In Progress'
    },
    {
        icon: NotificationIcon,
        title: 'Enhanced Notifications',
        description: 'Enhance the notification system for requested translations',
        date: '2025',
        status: 'Planned'
    },
    {
        icon: HistoryIcon,
        title: 'Translation History',
        description: 'Add functionality to view and manage translation history',
        date: '2025',
        status: 'Planned'
    },
    {
        icon: LanguageIcon,
        title: 'App Localization',
        description: 'Configurable localization options for the application',
        date: '2025',
        status: 'Planned'
    }
])
</script>
