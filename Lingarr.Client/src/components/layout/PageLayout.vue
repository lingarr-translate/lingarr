<template>
    <div
        :class="[`${instanceStore.getTheme}`, 'bg-primary text-primary-content flex min-h-screen']">
        <AsideNavigation />

        <div class="flex w-full flex-col drop-shadow-xl">
            <!-- Header -->
            <header class="bg-secondary px-4 py-2 shadow-lg sm:px-6 lg:px-8">
                <div class="flex w-full items-center justify-between space-x-2 md:justify-end">
                    <div class="md:hidden">
                        <MenuIcon class="block h-5 w-5 cursor-pointer" @click="isOpen = !isOpen" />
                    </div>
                    <div class="flex items-center justify-between">
                        <!--<LanguageSelect />-->
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
                                        class="hover:bg-secondary-focus text-secondary-content block w-full cursor-pointer px-4 py-2 text-left text-sm capitalize"
                                        @click="setTheme(theme)">
                                        {{ theme }}
                                    </button>
                                </div>
                            </template>
                        </DropdownComponent>
                    </div>
                </div>
            </header>
            <!-- Main Content -->
            <main class="flex-1">
                <slot></slot>
            </main>
        </div>
    </div>
</template>

<script setup lang="ts">
import { ref, Ref, computed, ComputedRef } from 'vue'
import { ITheme, THEMES } from '@/ts'
import { useInstanceStore } from '@/store/instance'
import AsideNavigation from '@/components/layout/AsideNavigation.vue'
import DropdownComponent from '@/components/common/DropdownComponent.vue'
import ThemeIcon from '@/components/icons/ThemeIcon.vue'
import MenuIcon from '@/components/icons/MenuIcon.vue'
// import LanguageSelect from '@/components/common/LanguageSelect.vue'

const instanceStore = useInstanceStore()
const themeDropdown: Ref = ref(false)

const isOpen: ComputedRef<boolean> = computed({
    get: () => instanceStore.getIsOpen,
    set: (value: boolean) => instanceStore.setIsOpen(value)
})

const setTheme = (theme: ITheme) => {
    instanceStore.storeTheme(theme)
    themeDropdown.value = false
}
</script>
