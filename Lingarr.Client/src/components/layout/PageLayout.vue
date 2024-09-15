<template>
    <div
        :class="[
            `${instanceStore.getTheme}`,
            'flex min-h-screen bg-primary text-primary-content '
        ]">
        <AsideNavigation />

        <div class="flex w-full flex-col">
            <!-- Header -->
            <header class="bg-secondary px-4 py-2 shadow-lg sm:px-6 lg:px-8">
                <div class="flex w-full items-center justify-between space-x-2 md:justify-end">
                    <MenuIcon
                        class="block h-5 w-5 cursor-pointer md:hidden"
                        @click="isOpen = !isOpen" />
                    <div ref="clickOutside" class="relative">
                        <button
                            class="hover:bg-secondary-focus rounded-full p-1 transition-colors"
                            @click="themeDropdown = !themeDropdown">
                            <svg
                                class="h-5 w-5 text-secondary-content"
                                viewBox="0 0 20 20"
                                fill="currentColor">
                                <path
                                    fill-rule="evenodd"
                                    d="M4 2a2 2 0 00-2 2v11a3 3 0 106 0V4a2 2 0 00-2-2H4zm1 14a1 1 0 100-2 1 1 0 000 2zm5-1.757l4.9-4.9a2 2 0 000-2.828L13.485 5.1a2 2 0 00-2.828 0L10 5.757v8.486zM16 18H9.071l6-6H16a2 2 0 012 2v2a2 2 0 01-2 2z"
                                    clip-rule="evenodd" />
                            </svg>
                        </button>
                        <div
                            v-if="themeDropdown"
                            class="absolute right-0 z-10 mt-2 w-48 rounded-md bg-secondary shadow-lg ring-1 ring-black ring-opacity-5">
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
                        </div>
                    </div>
                </div>
            </header>
            <!-- Main Content -->
            <main>
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
import useClickOutside from '@/composables/useClickOutside'
import MenuIcon from '@/components/icons/MenuIcon.vue'

const instanceStore = useInstanceStore()
const themeDropdown: Ref = ref(false)
const clickOutside: Ref<HTMLElement | undefined> = ref()

const isOpen: ComputedRef<boolean> = computed({
    get: () => instanceStore.getIsOpen,
    set: (value: boolean) => instanceStore.setIsOpen(value)
})

const setTheme = (theme: ITheme) => {
    instanceStore.storeTheme(theme)
    themeDropdown.value = false
}

useClickOutside(clickOutside, () => {
    if (themeDropdown.value) {
        themeDropdown.value = false
    }
})
</script>
