<template>
    <div
        :class="[`${instanceStore.getTheme}`, 'flex min-h-screen bg-primary text-primary-content']">
        <AsideNavigation />

        <div class="flex w-full flex-col">
            <!-- Header -->
            <header class="bg-secondary px-4 py-2 shadow-lg sm:px-6 lg:px-8">
                <div class="flex w-full items-center justify-between space-x-2 md:justify-end">
                    <MenuIcon
                        class="block h-5 w-5 cursor-pointer md:hidden"
                        @click="isOpen = !isOpen" />
                    <DropdownComponent width="medium">
                        <template #button>
                            <ThemeIcon class="h-5 w-5" />
                        </template>
                        <template #content>
                            <div
                                class="py-1"
                                aria-orientation="vertical"
                                role="menu"
                                aria-labelledby="options-menu">
                                <button
                                    v-for="theme in Object.values(THEMES)"
                                    :key="theme"
                                    class="hover:bg-secondary-focus block w-full px-4 py-2 text-left text-sm capitalize text-secondary-content"
                                    @click="setTheme(theme)">
                                    {{ theme }}
                                </button>
                            </div>
                        </template>
                    </DropdownComponent>

                    <DropdownComponent width="large">
                        <template #button>
                            <NotificationIcon class="h-5 w-5" />
                            <span
                                v-if="runningJobsCount > 0"
                                class="absolute right-1 top-1 inline-flex items-center justify-center rounded-full bg-accent px-1 py-0.5 text-xs font-bold leading-none text-secondary-content">
                                {{ runningJobsCount }}
                            </span>
                        </template>
                        <template #content>
                            <div v-if="runningJobs.length === 0" class="px-4 py-2 text-sm">
                                No running translations
                            </div>
                            <div v-else class="max-h-60 overflow-y-auto p-1">
                                <TranslationProgress
                                    v-for="job in runningJobs"
                                    :key="job.jobId"
                                    :job="job" />
                            </div>
                        </template>
                    </DropdownComponent>
                </div>
            </header>
            <!-- Main Content -->
            <main class="flex flex-grow">
                <slot></slot>
            </main>
        </div>
    </div>
</template>

<script setup lang="ts">
import { ref, Ref, computed, ComputedRef } from 'vue'
import { IRunningJob, ITheme, THEMES } from '@/ts'
import { useInstanceStore } from '@/store/instance'
import { useScheduleStore } from '@/store/schedule'
import AsideNavigation from '@/components/layout/AsideNavigation.vue'
import DropdownComponent from '@/components/common/DropdownComponent.vue'
import TranslationProgress from '@/components/common/TranslationProgress.vue'
import NotificationIcon from '@/components/icons/NotificationIcon.vue'
import ThemeIcon from '@/components/icons/ThemeIcon.vue'
import MenuIcon from '@/components/icons/MenuIcon.vue'

const instanceStore = useInstanceStore()
const scheduleStore = useScheduleStore()
const themeDropdown: Ref = ref(false)
const runningJobs: ComputedRef<IRunningJob[]> = computed(() => scheduleStore.getRunningJobs)
const runningJobsCount: ComputedRef<number> = computed(() => runningJobs.value.length)

const isOpen: ComputedRef<boolean> = computed({
    get: () => instanceStore.getIsOpen,
    set: (value: boolean) => instanceStore.setIsOpen(value)
})

const setTheme = (theme: ITheme) => {
    instanceStore.storeTheme(theme)
    themeDropdown.value = false
}
</script>
