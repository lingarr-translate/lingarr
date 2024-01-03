<template>
    <nav
        :class="{ 'hidden md:block': !isOpen }"
        class="absolute top-0 z-10 h-full w-64 border border-neutral-200 bg-white p-4 shadow-md md:relative md:rounded-xl md:border-0 md:bg-transparent md:shadow-none dark:border-neutral-800 dark:bg-neutral-700 dark:md:bg-transparent">
        <div class="absolute right-2 top-2 md:hidden" @click="isOpen = !isOpen">
            <CloseIcon />
        </div>
        <router-link
            :to="{ path: 'tvshows' }"
            class="flex w-full cursor-pointer items-center rounded-xl p-2 text-base transition duration-300 ease-in-out hover:shadow-sm hover:md:bg-white hover:dark:bg-neutral-700"
            :class="{
                'bg-blue-300/20 shadow-sm md:bg-white dark:bg-neutral-600': route.name === 'tvshows'
            }">
            TV Shows
        </router-link>
        <router-link
            :to="{ path: 'movies' }"
            class="flex w-full cursor-pointer items-center rounded-xl p-2 text-base transition duration-300 ease-in-out hover:shadow-sm hover:md:bg-white hover:dark:bg-neutral-700"
            :class="{
                'bg-blue-300/20 shadow-sm md:bg-white dark:bg-neutral-600': route.name === 'movies'
            }">
            Movies
        </router-link>
        <router-link
            :to="{ path: 'settings' }"
            class="flex w-full cursor-pointer items-center rounded-xl p-2 text-base transition duration-300 ease-in-out hover:shadow-sm hover:md:bg-white hover:dark:bg-neutral-700"
            :class="{
                'bg-blue-300/20 shadow-sm md:bg-white dark:bg-neutral-600':
                    route.name === 'settings'
            }">
            Settings
        </router-link>
    </nav>
</template>

<script lang="ts" setup>
import { computed, WritableComputedRef } from 'vue'
import { useRoute } from 'vue-router'
import { useInstanceStore } from '@/store/instance'
import CloseIcon from '@/components/icons/CloseIcon.vue'

const route = useRoute()
const instanceStore = useInstanceStore()

const isOpen: WritableComputedRef<boolean> = computed({
    get: () => instanceStore.getMenuIsOpen,
    set: (value: boolean) => {
        instanceStore.setMenuIsOpen(value)
    }
})
</script>
