<template>
    <CardComponent title="Lingarr Roadmap" description="">
        <template #description>Improvements, both completed and planned.</template>
        <template #content>
            <div v-for="(item, index) in roadmapItems" :key="index" class="flex justify-center">
                <div
                    class="mr-4 mt-2 h-4 w-4"
                    :class="{
                        'brightness-50': item.fade
                    }">
                    <component
                        :is="item.icon"
                        v-if="item.icon"
                        class="h-4 w-4 text-primary-content" />
                </div>
                <div class="flex-grow">
                    <h3
                        class="text-md font-semibold text-primary-content"
                        :class="{
                            'brightness-50': item.fade
                        }">
                        {{ item.title }}
                    </h3>
                    <p
                        class="text-sm text-secondary-content/50"
                        :class="{
                            'brightness-50': item.fade
                        }">
                        {{ item.description }}
                    </p>
                    <p
                        class="mt-1 text-xs text-tertiary-content"
                        :class="{
                            'brightness-50': item.fade
                        }">
                        {{ item.date }}
                    </p>
                </div>
                <div class="ml-4 flex-shrink-0">
                    <span
                        class="rounded-full px-2 py-1 text-xs font-medium"
                        :class="[
                            item.status === 'Completed'
                                ? 'bg-green-500 text-green-100'
                                : item.status === 'In Progress'
                                  ? 'bg-blue-500 text-blue-100'
                                  : 'bg-gray-600 text-gray-100',
                            item.fade ? 'brightness-50' : ''
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
import IntegrationIcon from '@/components/icons/IntegrationIcon.vue'
import StatisticsIcon from '@/components/icons/StatisticsIcon.vue'
import HomeIcon from '@/components/icons/HomeIcon.vue'

const roadmapItems = shallowRef([
    {
        icon: BuildingIcon,
        title: 'Application Rebuild',
        status: 'Completed',
        fade: true
    },
    {
        icon: NotificationIcon,
        title: 'Notifications',
        status: 'Completed',
        fade: true
    },
    {
        icon: AutomationIcon,
        title: 'Automated Translation',
        status: 'Completed',
        fade: true
    },
    {
        icon: IntegrationIcon,
        title: 'AI Translation',
        status: 'Completed',
        fade: true
    },
    {
        icon: NotificationIcon,
        title: 'Enhanced Notifications',
        description: 'Enhance the notification system for requested translations',
        date: '2024',
        status: 'In Progress',
        fade: false
    },
    {
        icon: HistoryIcon,
        title: 'Translation History',
        description: 'Add functionality to view and manage translation history',
        date: '2024',
        status: 'In Progress',
        fade: false
    },
    {
        icon: AutomationIcon,
        title: 'Schedule controls',
        description: 'Adding support for controlling schedules',
        date: '2024',
        status: 'Planned',
        fade: false
    },
    {
        icon: LanguageIcon,
        title: 'App Localization',
        description: 'Configurable localization options for the application',
        date: '2024',
        status: 'Planned',
        fade: false
    },
    {
        icon: StatisticsIcon,
        title: 'Dashboard improvements',
        description: 'Statistics and dashboard improvements',
        date: '2024',
        status: 'Planned',
        fade: false
    },
    {
        icon: HomeIcon,
        title: 'Version 1.0 release',
        description: 'Production version released with a reduced number of breaking changes.',
        date: '2024 - 2025',
        status: 'Planned',
        fade: false
    }
])
</script>
