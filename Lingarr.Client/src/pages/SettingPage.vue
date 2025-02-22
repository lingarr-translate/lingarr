<template>
    <PageLayout>
        <div class="grid h-full grid-cols-[auto_1fr]">
            <aside class="bg-secondary w-[3.175rem] shrink-0 md:w-40">
                <nav class="pt-4 md:pt-8 md:pl-4">
                    <ul class="flex flex-col space-y-4">
                        <li
                            v-for="(item, index) in menuItems"
                            :key="index"
                            class="w-full hover:brightness-150"
                            :class="[
                                'w-full hover:brightness-150',
                                { 'brightness-150': $route.name === item.route }
                            ]">
                            <router-link
                                :to="{ name: item.route }"
                                :title="item.label"
                                :aria-label="item.label"
                                class="flex w-full cursor-pointer items-center justify-center md:justify-start">
                                <component :is="item.icon" class="h-4 w-4 md:mr-3" />
                                <span class="hidden text-sm md:inline-block">
                                    {{ item.label }}
                                </span>
                            </router-link>
                        </li>
                    </ul>
                </nav>
            </aside>

            <main class="flex">
                <router-view></router-view>
            </main>
        </div>
    </PageLayout>
</template>

<script setup lang="ts">
import { useI18n } from '@/plugins/i18n'
import PageLayout from '@/components/layout/PageLayout.vue'
import IntegrationIcon from '@/components/icons/IntegrationIcon.vue'
import SettingIcon from '@/components/icons/SettingIcon.vue'
import AutomationIcon from '@/components/icons/AutomationIcon.vue'
import TaskIcon from '@/components/icons/TaskIcon.vue'
import { MenuItem } from '@/ts'

const { translate } = useI18n()

const menuItems: MenuItem[] = [
    {
        label: translate('navigation.integrations'),
        icon: IntegrationIcon,
        route: 'integration-settings',
        children: []
    },
    {
        label: translate('navigation.services'),
        icon: SettingIcon,
        route: 'services-settings',
        children: []
    },
    {
        label: translate('navigation.automation'),
        icon: AutomationIcon,
        route: 'automation-settings',
        children: []
    },
    { label: translate('navigation.tasks'), icon: TaskIcon, route: 'tasks-settings', children: [] }
]
</script>
