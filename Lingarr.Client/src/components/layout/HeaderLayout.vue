<template>
    <header class="flex items-center justify-between p-4">
        <div @click="menuIsOpen = !menuIsOpen" class="md:hidden">
            <MenuIcon />
        </div>
        <div class="flex items-center">
            <span
                class="mr-4 flex h-6 w-6 items-center justify-center rounded-full bg-blue-400 text-xs font-bold text-white shadow-sm dark:bg-blue-500">
                L
            </span>
            <p class="hidden text-lg font-semibold md:block dark:text-neutral-200">Lingarr</p>
            <div class="relative ml-2 md:ml-8">
                <input
                    v-model="searchFilter"
                    class="rounded-xl bg-white px-3 py-2 pl-10 shadow-sm focus:border-blue-300 focus:outline-none focus:ring dark:bg-neutral-800 dark:text-neutral-200"
                    type="text"
                    placeholder="Search" />
                <span class="absolute left-2 top-2 text-neutral-400">🔍</span>
            </div>
        </div>
        <button
            class="cursor-pointer rounded-xl bg-white p-2 shadow-sm active:bg-white/60 dark:bg-neutral-700 active:dark:bg-neutral-700/60"
            title="Toggle Theme"
            @click="useInstanceStore().setTheme()">
            🌞
        </button>
    </header>
</template>

<script setup lang="ts">
import { computed, WritableComputedRef } from 'vue'
import { useInstanceStore } from '@/store/instance'
import { useResourceStore } from '@/store/resource'
import MenuIcon from '@/components/icons/MenuIcon.vue'

const instanceStore = useInstanceStore()
const resourceStore = useResourceStore()

const menuIsOpen: WritableComputedRef<boolean> = computed({
    get: () => instanceStore.getMenuIsOpen,
    set: (value: boolean) => {
        instanceStore.setMenuIsOpen(value)
    }
})
const searchFilter: WritableComputedRef<string> = computed({
    get: () => resourceStore.getFilter,
    set: (value: string) => {
        resourceStore.setFilter(value)
    }
})
</script>
