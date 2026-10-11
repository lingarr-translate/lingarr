<template>
    <div :class="[`${instanceStore.getTheme}`, 'bg-primary text-primary-content flex min-h-dvh']">
        <a href="#main-content" class="skip-link" @click.prevent="skipToContent">Skip to content</a>
        <AsideNavigation />

        <div :inert="isOpen" class="flex min-w-0 flex-1 flex-col">
            <header
                class="ui-shell ui-border flex min-h-24 items-center gap-3 border-b px-4 py-4 sm:px-6 lg:px-8">
                <button
                    id="navigation-toggle"
                    type="button"
                    class="ui-icon-button md:hidden"
                    aria-label="Open navigation"
                    aria-controls="main-navigation"
                    :aria-expanded="isOpen"
                    @click="isOpen = true">
                    <MenuIcon class="h-5 w-5" />
                </button>
                <div class="min-w-0 flex-1">
                    <h1 class="text-xl font-semibold tracking-tight sm:text-2xl">
                        {{ pageTitle }}
                    </h1>
                    <p class="text-secondary-content mt-0.5 text-sm">{{ pageDescription }}</p>
                </div>
                <DropdownComponent
                    width="large"
                    label="Choose color theme"
                    button-class="theme-trigger">
                    <template #button>
                        <ThemeIcon class="h-5 w-5" />
                        <span class="hidden text-sm font-medium sm:inline">Theme</span>
                    </template>
                    <template #content>
                        <div class="dashboard-divider shrink-0 border-b px-4 py-4">
                            <h2 class="text-sm font-semibold">Appearance</h2>
                            <p class="text-secondary-content mt-1 text-xs">Choose a color theme</p>
                        </div>
                        <div class="grid max-h-112 min-h-0 grid-cols-2 gap-2 overflow-y-auto p-3">
                            <button
                                v-for="theme in Object.values(THEMES)"
                                :key="theme"
                                type="button"
                                :aria-label="theme.replaceAll('-', ' ')"
                                :aria-pressed="instanceStore.getTheme === theme"
                                :class="{ 'is-selected': instanceStore.getTheme === theme }"
                                class="theme-option min-w-0 cursor-pointer rounded-lg p-2.5 text-left"
                                @click="setTheme(theme)">
                                <span
                                    :class="theme"
                                    class="theme-swatch mb-2 flex h-8 items-center gap-1.5 rounded-md px-2"
                                    aria-hidden="true">
                                    <span class="bg-secondary h-4 w-4 rounded-full"></span>
                                    <span class="bg-primary-content h-4 w-4 rounded-full"></span>
                                    <span class="bg-accent h-4 w-4 rounded-full"></span>
                                </span>
                                <span
                                    class="flex items-center justify-between gap-1 text-xs capitalize">
                                    {{ theme.replaceAll('-', ' ') }}
                                    <span
                                        v-if="instanceStore.getTheme === theme"
                                        aria-hidden="true">
                                        ✓
                                    </span>
                                </span>
                            </button>
                        </div>
                    </template>
                </DropdownComponent>
            </header>
            <!-- Main Content -->
            <main id="main-content" ref="mainContent" tabindex="-1" class="min-w-0 flex-1">
                <router-view v-slot="{ Component }">
                    <transition name="fade" mode="out-in">
                        <component :is="Component" />
                    </transition>
                </router-view>
            </main>
        </div>
    </div>
</template>

<script setup lang="ts">
import { ref, onMounted, computed, onUnmounted } from 'vue'
import { useRoute } from 'vue-router'
import { useSignalR } from '@/composables/useSignalR'
import { Hub, IActiveTranslation, ISettings, ITheme, THEMES } from '@/ts'
import { useSettingStore } from '@/store/setting'
import { useInstanceStore } from '@/store/instance'
import useTranslationRequestStore from '@/store/translationRequest'

import AsideNavigation from '@/components/layout/AsideNavigation.vue'
import DropdownComponent from '@/components/common/DropdownComponent.vue'
import ThemeIcon from '@/components/icons/ThemeIcon.vue'
import MenuIcon from '@/components/icons/MenuIcon.vue'

const settingStore = useSettingStore()
const instanceStore = useInstanceStore()
const translationRequestStore = useTranslationRequestStore()
const signalR = useSignalR()
const route = useRoute()
const mainContent = ref<HTMLElement>()
const skipToContent = () => {
    // Stay in this document even when the server injects a different base URL.
    mainContent.value?.focus({ preventScroll: true })
    mainContent.value?.scrollIntoView({ block: 'start' })
}
const pageInfo: Record<string, [string, string]> = {
    dashboard: ['Dashboard', 'Your media library and translation activity at a glance.'],
    movies: ['Movies', 'Manage subtitles and translation preferences for your movies.'],
    shows: ['TV Shows', 'Browse seasons, episodes and available subtitles.'],
    translations: ['Translations', 'Follow progress and manage your translation requests.'],
    'translation-detail': [
        'Translation details',
        'Review progress, translated subtitles and results.'
    ]
}
const pageTitle = computed(() => pageInfo[String(route.name)]?.[0] ?? 'Settings')
const pageDescription = computed(
    () =>
        pageInfo[String(route.name)]?.[1] ?? 'Connect your services and make Lingarr work for you.'
)
const settingHubConnection = ref<Hub>()
const requestHubConnection = ref<Hub>()

const onSettingUpdate = (setting: { key: keyof ISettings; value: string }) => {
    settingStore.storeSetting(setting.key, setting.value)
}

const onActiveTranslations = (activeTranslations: IActiveTranslation[]) => {
    translationRequestStore.setActiveTranslations(activeTranslations)
}

const setTheme = (theme: ITheme) => {
    instanceStore.storeTheme(theme)
}

const isOpen = computed({
    get: () => instanceStore.getIsOpen,
    set: (value: boolean) => instanceStore.setIsOpen(value)
})

onMounted(async () => {
    await settingStore.applySettingsOnLoad()
    await instanceStore.applyVersionOnLoad()
    await translationRequestStore.fetchActiveTranslations()

    settingHubConnection.value = await signalR.connect('SettingUpdates', '/signalr/SettingUpdates')
    await settingHubConnection.value.joinGroup({ group: 'SettingUpdates' })
    settingHubConnection.value.on('SettingUpdate', onSettingUpdate)

    requestHubConnection.value = await signalR.connect(
        'TranslationRequests',
        '/signalr/TranslationRequests'
    )
    await requestHubConnection.value.joinGroup({ group: 'TranslationRequests' })
    requestHubConnection.value.on('ActiveTranslations', onActiveTranslations)
})

onUnmounted(async () => {
    settingHubConnection.value?.off('SettingUpdate', onSettingUpdate)
    requestHubConnection.value?.off('ActiveTranslations', onActiveTranslations)
})
</script>
