<template>
    <div class="flex min-w-0 flex-col xl:h-full xl:flex-row">
        <aside
            class="ui-border bg-secondary min-w-0 border-b xl:w-52 xl:shrink-0 xl:border-r xl:border-b-0">
            <nav
                ref="settingsNavigation"
                aria-label="Settings"
                class="overflow-x-auto p-3 xl:sticky xl:top-0 xl:p-4">
                <ul class="flex w-max gap-1 xl:w-full xl:flex-col">
                    <li v-for="item in menuItems" :key="item.route">
                        <router-link
                            :to="{ name: item.route }"
                            :aria-current="$route.name === item.route ? 'page' : undefined"
                            :class="{ 'is-active': $route.name === item.route }"
                            class="ui-nav-link text-sm whitespace-nowrap">
                            <component :is="item.icon" class="h-4 w-4 shrink-0" />
                            <span>{{ item.label }}</span>
                        </router-link>
                    </li>
                    <li v-if="settings.getSetting(SETTINGS.AUTH_ENABLED) == 'true'">
                        <button
                            type="button"
                            class="ui-nav-link w-full text-sm"
                            @click="handleLogout">
                            <LogoutIcon class="h-4 w-4" />
                            Logout
                        </button>
                    </li>
                </ul>
            </nav>
        </aside>
        <div class="min-w-0 flex-1">
            <router-view v-slot="{ Component }">
                <transition name="fade" mode="out-in">
                    <component :is="Component" />
                </transition>
            </router-view>
        </div>
    </div>
</template>

<script setup lang="ts">
import { ref, watch, nextTick } from 'vue'
import { useRoute, useRouter } from 'vue-router'
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
import TelemetryIcon from '@/components/icons/TelemetryIcon.vue'

const router = useRouter()
const route = useRoute()
const settingsNavigation = ref<HTMLElement>()
watch(
    () => route.name,
    async () => {
        await nextTick()
        const nav = settingsNavigation.value
        const active = nav?.querySelector<HTMLElement>('[aria-current="page"]')
        if (nav && active && nav.scrollWidth > nav.clientWidth) {
            nav.scrollLeft +=
                active.getBoundingClientRect().left - nav.getBoundingClientRect().left - 12
        }
    },
    { immediate: true }
)
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
    {
        label: 'Plugins',
        icon: SettingIcon,
        route: 'plugins-settings',
        children: []
    },
    { label: 'Tasks', icon: TaskIcon, route: 'tasks-settings', children: [] },
    { label: 'Logs', icon: LogIcon, route: 'logs-settings', children: [] },
    { label: 'Telemetry', icon: TelemetryIcon, route: 'telemetry-settings', children: [] }
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
