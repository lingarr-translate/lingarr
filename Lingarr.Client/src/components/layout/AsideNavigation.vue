<template>
    <div class="relative">
        <!-- Backdrop -->
        <div
            v-if="isOpen"
            class="fixed inset-0 z-40 bg-black bg-opacity-50 md:hidden"
            @click="isOpen = false"></div>
        <!-- Aside -->
        <aside
            :class="isOpen ? 'translate-x-0' : '-translate-x-full md:translate-x-0'"
            class="fixed left-0 top-0 z-50 flex h-full w-64 flex-col overflow-hidden border-r border-accent bg-secondary transition-transform duration-300 ease-in-out md:sticky">
            <TimesIcon
                class="absolute right-1 top-1 block h-6 w-6 cursor-pointer md:hidden"
                @click="isOpen = false" />
            <div class="flex h-16 items-center justify-center">
                <h1 class="text-xl font-bold">Lingarr</h1>
            </div>
            <!-- Navigation -->
            <nav class="flex-grow overflow-y-auto p-6">
                <ul class="space-y-4">
                    <li
                        v-for="(item, index) in menuItems"
                        :key="index"
                        class="flex w-full cursor-pointer items-center justify-start"
                        @click="navigate(item.route)">
                        <component :is="item.icon" class="mr-2 h-4 w-4" />
                        {{ item.label }}
                    </li>
                </ul>
            </nav>
            <!-- Version and media section -->
            <div class="pointer-events-none h-64 w-full">
                <img
                    v-if="instanceStore.getPoster"
                    :src="`/api/image/${instanceStore.getPoster}`"
                    class="h-full w-full object-cover mask-gradient"
                    alt="poster" />
                <div
                    v-if="instanceStore.getVersion.currentVersion.length"
                    class="absolute bottom-0 right-0 flex w-full justify-center p-4">
                    <BadgeComponent
                        v-if="instanceStore.getVersion.newVersion"
                        classes="text-white border-green-200 bg-green-500/50">
                        Update {{ instanceStore.getVersion.latestVersion }} is available
                    </BadgeComponent>
                    <BadgeComponent v-else>
                        Version: {{ instanceStore.getVersion.currentVersion }}
                    </BadgeComponent>
                </div>
            </div>
        </aside>
    </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { useRouter } from 'vue-router'
import { useInstanceStore } from '@/store/instance'
import HomeIcon from '@/components/icons/HomeIcon.vue'
import MovieIcon from '@/components/icons/MovieIcon.vue'
import ShowIcon from '@/components/icons/ShowIcon.vue'
import SettingIcon from '@/components/icons/SettingIcon.vue'
import TimesIcon from '@/components/icons/TimesIcon.vue'
import BadgeComponent from '@/components/common/BadgeComponent.vue'

const instanceStore = useInstanceStore()
const router = useRouter()

const isOpen = computed({
    get: () => instanceStore.getIsOpen,
    set: (value) => instanceStore.setIsOpen(value)
})

const menuItems = [
    { label: 'Dashboard', icon: HomeIcon, route: 'dashboard' },
    { label: 'Movies', icon: MovieIcon, route: 'movies' },
    { label: 'TV Shows', icon: ShowIcon, route: 'shows' },
    { label: 'Settings', icon: SettingIcon, route: 'settings' }
]

function navigate(route: string) {
    isOpen.value = false
    router.push({ name: route })
}
</script>
