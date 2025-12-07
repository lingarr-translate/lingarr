<template>
    <div class="grid h-full grid-cols-[auto_1fr]">
        <aside class="bg-secondary w-[3.175rem] shrink-0 md:w-40">
            <nav class="flex h-full flex-col pt-4 md:pt-8 md:pl-4">
                <ul class="flex flex-col space-y-4">
                    <li
                        v-for="(item, index) in menuItems"
                        :key="index"
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
                    <li
                        v-if="settings.getSetting(SETTINGS.AUTH_ENABLED) == 'true'"
                        class="text-primary-content/50 w-full hover:brightness-150"
                        @click="handleLogout">
                        <div
                            class="flex w-full cursor-pointer items-center justify-center md:justify-start">
                            <LogoutIcon class="h-4 w-4 md:mr-3" />
                            <span class="hidden text-sm md:inline-block">Logout</span>
                        </div>
                    </li>
                </ul>
            </nav>
        </aside>

        <main class="flex">
            <router-view v-slot="{ Component }">
                <transition name="fade" mode="out-in">
                    <component :is="Component" />
                </transition>
            </router-view>
        </main>
    </div>
</template>

<script setup lang="ts">
import { useRouter } from 'vue-router'
import { MenuItem, SETTINGS } from '@/ts'
import { useSettingStore } from '@/store/setting'
import services from '@/services'
import IntegrationIcon from '@/components/icons/IntegrationIcon.vue'
import KeyIcon from '@/components/icons/KeyIcon.vue'
import SettingIcon from '@/components/icons/SettingIcon.vue'
import AutomationIcon from '@/components/icons/AutomationIcon.vue'
import TaskIcon from '@/components/icons/TaskIcon.vue'
import LanguageIcon from '@/components/icons/LanguageIcon.vue'
import LogIcon from '@/components/icons/LogIcon.vue'
import LogoutIcon from '@/components/icons/LogoutIcon.vue'

const router = useRouter()
const settings = useSettingStore()

const menuItems: MenuItem[] = [
    {
        label: 'Integrations',
        icon: IntegrationIcon,
        route: 'integration-settings',
        children: []
    },
    {
        label: 'Authentication',
        icon: KeyIcon,
        route: 'authentication-settings',
        children: []
    },
    {
        label: 'Services',
        icon: SettingIcon,
        route: 'services-settings',
        children: []
    },
    {
        label: 'Subtitle',
        icon: LanguageIcon,
        route: 'subtitle-settings',
        children: []
    },
    {
        label: 'Automation',
        icon: AutomationIcon,
        route: 'automation-settings',
        children: []
    },
    { label: 'Tasks', icon: TaskIcon, route: 'tasks-settings', children: [] },
    { label: 'Logs', icon: LogIcon, route: 'logs-settings', children: [] }
]

const handleLogout = async () => {
    try {
        await services.auth.logout()
        router.push({ name: 'login' })
    } catch (error) {
        console.error('Logout failed:', error)
    }
}
</script>
