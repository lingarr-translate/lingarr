<template>
    <div class="relative shrink-0 md:w-60">
        <!-- Backdrop -->
        <div
            v-if="isOpen"
            class="fixed inset-0 z-40 bg-black/60 backdrop-blur-sm md:hidden"
            @click="isOpen = false"></div>
        <!-- Aside -->
        <aside
            id="main-navigation"
            ref="navigation"
            :inert="isMobile && !isOpen"
            aria-label="Main navigation"
            @keydown="handleKeydown"
            :class="isOpen ? 'translate-x-0' : '-translate-x-full md:translate-x-0'"
            class="ui-shell ui-border fixed top-0 left-0 z-50 flex h-dvh w-64 flex-col overflow-hidden border-r transition-transform duration-200 ease-in-out md:sticky md:w-60">
            <div
                class="navigation-header ui-border flex min-h-24 shrink-0 items-center gap-3 border-b px-6">
                <FeatureIcon kind="translations" class="h-10 w-10" />
                <div>
                    <span class="text-xl font-bold tracking-tight">Lingarr</span>
                    <p class="text-secondary-content text-xs">Subtitle translation</p>
                </div>
                <button
                    type="button"
                    class="ui-icon-button absolute top-2 right-2 md:hidden"
                    aria-label="Close navigation"
                    @click="isOpen = false">
                    <TimesIcon class="h-5 w-5" />
                </button>
            </div>
            <!-- Navigation -->
            <nav
                aria-label="Main"
                class="navigation-links min-h-0 flex-1 overflow-y-auto px-3 py-6">
                <ul class="space-y-1.5">
                    <li v-for="(item, index) in menuItems" :key="index">
                        <router-link
                            :to="{ name: item.route }"
                            class="ui-nav-link"
                            :aria-current="isActive(item) ? 'page' : undefined"
                            :class="{ 'is-active': isActive(item) }"
                            @click="isOpen = false">
                            <FeatureIcon :kind="item.route" class="h-9 w-9" />
                            <div class="flex flex-1 items-center justify-between">
                                {{ item.label }}

                                <span
                                    v-if="item.route == 'translations' && activeRequests > 0"
                                    class="bg-accent text-accent-content inline-flex min-w-6 items-center justify-center rounded-full px-1.5 py-0.5 text-xs font-bold">
                                    {{ activeRequests }}
                                </span>
                            </div>
                        </router-link>
                    </li>
                </ul>
            </nav>
            <!-- Version and media section -->
            <div
                class="navigation-footer pointer-events-none relative flex w-full shrink-0 items-end"
                :class="{ 'has-poster': instanceStore.getPoster }">
                <img
                    v-if="instanceStore.getPoster"
                    :src="`/api/image/${instanceStore.getPoster}`"
                    class="mask-gradient absolute inset-0 h-full w-full object-cover"
                    alt="poster" />
                <div
                    v-if="instanceStore.getVersion.currentVersion.length"
                    class="relative flex w-full justify-center p-4">
                    <BadgeComponent
                        v-if="instanceStore.getVersion.isDevelopment"
                        classes="text-white border-amber-200 bg-amber-500/50">
                        {{ 'Development' }}
                    </BadgeComponent>
                    <BadgeComponent
                        v-else-if="instanceStore.getVersion.newVersion"
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
import { computed, ComputedRef, ref, watch, nextTick, onMounted, onUnmounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
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
import FeatureIcon from '@/components/common/FeatureIcon.vue'

const translationRequestStore = useTranslationRequestStore()
const instanceStore = useInstanceStore()
const router = useRouter()
const route = useRoute()

const activeRequests: ComputedRef<number> = computed(
    () => translationRequestStore.getActiveTranslationCount
)

const isOpen = computed({
    get: () => instanceStore.getIsOpen,
    set: (value) => instanceStore.setIsOpen(value)
})

const navigation = ref<HTMLElement>()
const isMobile = ref(window.matchMedia('(max-width: 767px)').matches)
let previousFocus: HTMLElement | null = null
let previousOverflow = ''
const mediaQuery = window.matchMedia('(max-width: 767px)')
const handleResize = () => {
    isMobile.value = mediaQuery.matches
    if (!isMobile.value) isOpen.value = false
}
watch(isOpen, async (open) => {
    if (open && isMobile.value) {
        previousFocus = document.activeElement as HTMLElement
        previousOverflow = document.body.style.overflow
        document.body.style.overflow = 'hidden'
        await nextTick()
        navigation.value?.querySelector<HTMLButtonElement>('button')?.focus()
    } else {
        document.body.style.overflow = previousOverflow
        await nextTick()
        if (isMobile.value) previousFocus?.focus()
    }
})
const handleKeydown = (event: KeyboardEvent) => {
    if (!isOpen.value || !isMobile.value) return
    if (event.key === 'Escape') isOpen.value = false
    if (event.key !== 'Tab') return
    const items = navigation.value?.querySelectorAll<HTMLElement>('a[href], button:not([disabled])')
    if (!items?.length) return
    const first = items[0]
    const last = items[items.length - 1]
    if (event.shiftKey && document.activeElement === first) {
        event.preventDefault()
        last.focus()
    } else if (!event.shiftKey && document.activeElement === last) {
        event.preventDefault()
        first.focus()
    }
}
onMounted(() => mediaQuery.addEventListener('change', handleResize))
onUnmounted(() => {
    mediaQuery.removeEventListener('change', handleResize)
    document.body.style.overflow = previousOverflow
})

const menuItems: MenuItem[] = [
    { label: 'Dashboard', icon: HomeIcon, route: 'dashboard', children: [] },
    { label: 'Movies', icon: MovieIcon, route: 'movies', children: [] },
    { label: 'TV Shows', icon: ShowIcon, route: 'shows', children: [] },
    { label: 'Translations', icon: LanguageIcon, route: 'translations', children: [] },
    {
        label: 'Settings',
        icon: SettingIcon,
        route: 'integration-settings',
        children:
            router
                .getRoutes()
                .find((r) => r.name === 'settings')
                ?.children.map((c) => c.name as string) ?? []
    }
]

const isActive = (item: MenuItem) => {
    if (item.route == route.name) return true
    if (item.route === 'translations' && route.name === 'translation-detail') return true

    if (item.children?.length) {
        return item.children.includes(route.name as string)
    }
    return false
}
</script>

<style scoped>
.navigation-footer.has-poster {
    min-height: 12rem;
}

/* Keep decorative content from crowding the menu in short windows. */
@media (max-height: 640px) {
    .navigation-header {
        min-height: 4rem;
    }

    .navigation-links {
        padding-block: 0.75rem;
    }

    .navigation-footer.has-poster {
        min-height: 0;
    }
}
</style>
