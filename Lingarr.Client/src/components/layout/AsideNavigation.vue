<template>
    <div class="relative">
        <!-- Backdrop -->
        <div
            v-if="isOpen"
            class="bg-opacity-50 fixed inset-0 z-40 bg-black md:hidden"
            @click="isOpen = false"></div>
        <!-- Aside -->
        <aside
            :class="isOpen ? 'translate-x-0' : '-translate-x-full md:translate-x-0'"
            class="border-accent bg-secondary fixed top-0 left-0 z-50 flex h-full w-64 flex-col overflow-hidden border-r transition-transform duration-300 ease-in-out md:sticky">
            <TimesIcon
                class="absolute top-1 right-1 block h-6 w-6 cursor-pointer md:hidden"
                @click="isOpen = false" />
            <div class="flex h-16 items-center justify-center">
                <h1 class="text-xl font-bold">Lingarr</h1>
            </div>
            <!-- Navigation -->
            <nav class="grow overflow-y-auto p-6">
                <ul class="space-y-4">
                    <li v-for="(item, index) in menuItems" :key="index">
                        <router-link
                            :to="{ name: item.route }"
                            class="flex w-full items-center justify-start hover:brightness-150"
                            :class="{ 'brightness-150': isActive(item) }"
                            @click="isOpen = false">
                            <component :is="item.icon" class="mr-2 h-4 w-4" />
                            <div class="relative">
                                {{ item.label }}

                                <span
                                    v-if="item.route == 'translations' && activeRequests > 0"
                                    class="bg-accent text-secondary-content absolute -top-1 -right-4 inline-flex items-center justify-center rounded-full px-1 py-0.5 text-xs leading-none font-bold">
                                    {{ activeRequests }}
                                </span>
                            </div>
                        </router-link>
                    </li>
                </ul>
            </nav>
            <!-- Version and media section -->
            <div class="pointer-events-none h-64 w-full">
                <img
                    v-if="instanceStore.getPoster"
                    :src="`/api/image/${instanceStore.getPoster}`"
                    class="mask-gradient h-full w-full object-cover"
                    alt="poster" />
                <div
                    v-if="instanceStore.getVersion.currentVersion.length"
                    class="absolute right-0 bottom-0 flex w-full justify-center p-4">
                    <BadgeComponent
                        v-if="instanceStore.getVersion.newVersion"
                        classes="text-white border-green-200 bg-green-500/50">
                        {{
                            'Update {version} is available'.format({
                                version: instanceStore.getVersion.latestVersion
                            })
                        }}
                    </BadgeComponent>
                    <BadgeComponent v-else>
                        {{
                            'Version: {version}'.format({
                                version: instanceStore.getVersion.currentVersion
                            })
                        }}
                    </BadgeComponent>
                </div>
            </div>
        </aside>
    </div>
</template>

<script setup lang="ts">
import { computed, ComputedRef } from 'vue'
import { useRoute } from 'vue-router'
import { useInstanceStore } from '@/store/instance'
import { useTranslationRequestStore } from '@/store/translationRequest'
import { MenuItem } from '@/ts'
import HomeIcon from '@/components/icons/HomeIcon.vue'
import MovieIcon from '@/components/icons/MovieIcon.vue'
import ShowIcon from '@/components/icons/ShowIcon.vue'
import SettingIcon from '@/components/icons/SettingIcon.vue'
import TimesIcon from '@/components/icons/TimesIcon.vue'
import BadgeComponent from '@/components/common/BadgeComponent.vue'
import LanguageIcon from '@/components/icons/LanguageIcon.vue'

const translationRequestStore = useTranslationRequestStore()
const instanceStore = useInstanceStore()
const route = useRoute()

const activeRequests: ComputedRef<number> = computed(
    () => translationRequestStore.getActiveTranslationRequests
)

const isOpen = computed({
    get: () => instanceStore.getIsOpen,
    set: (value) => instanceStore.setIsOpen(value)
})

const menuItems: MenuItem[] = [
    { label: 'Dashboard', icon: HomeIcon, route: 'dashboard', children: [] },
    { label: 'Movies', icon: MovieIcon, route: 'movies', children: [] },
    { label: 'TV Shows', icon: ShowIcon, route: 'shows', children: [] },
    {
        label: 'Translations',
        icon: LanguageIcon,
        route: 'translations',
        children: []
    },
    {
        label: 'Settings',
        icon: SettingIcon,
        route: 'integration-settings',
        children: [
            'integration-settings',
            'services-settings',
            'automation-settings',
            'tasks-settings',
            'logs-settings'
        ]
    }
]

const isActive = (item: MenuItem) => {
    if (item.route == route.name) return true

    if (item.children?.length) {
        return item.children.includes(route.name as string)
    }
    return false
}
</script>
