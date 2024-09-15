<template>
    <aside
        :class="isOpen ? 'absolute z-10 ' : 'hidden'"
        class="top-0 h-screen w-64 overflow-hidden border-r border-accent bg-secondary md:sticky md:block">
        <CloseIcon
            class="absolute right-1 top-1 block h-6 w-6 cursor-pointer md:hidden"
            @click="isOpen = !isOpen" />
        <div class="flex h-16 items-center justify-center">
            <h1 class="text-xl font-bold">Lingarr</h1>
        </div>
        <nav class="p-6">
            <ul class="space-y-4">
                <li
                    class="flex w-full cursor-pointer items-center justify-start"
                    @click="navigate('dashboard')">
                    <HomeIcon class="mr-2 h-4 w-4" />
                    Dashboard
                </li>
                <li
                    class="flex w-full cursor-pointer items-center justify-start"
                    @click="navigate('movies')">
                    <MovieIcon class="mr-2 h-4 w-4" />
                    Movies
                </li>
                <li
                    class="flex w-full cursor-pointer items-center justify-start"
                    @click="navigate('shows')">
                    <ShowIcon class="mr-2 h-4 w-4" />
                    TV Shows
                </li>
                <li
                    class="flex w-full cursor-pointer items-center justify-start"
                    @click="navigate('settings')">
                    <SettingIcon class="mr-2 h-4 w-4" />
                    Settings
                </li>
            </ul>
        </nav>
        <div
            v-if="instanceStore.getPoster"
            class="pointer-events-none absolute bottom-0 h-64 w-full">
            <img
                :src="`/api/image/${instanceStore.getPoster}`"
                class="h-full w-full object-cover mask-gradient"
                alt="poster" />
        </div>
    </aside>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { useRouter } from 'vue-router'
import { useInstanceStore } from '@/store/instance'
import HomeIcon from '@/components/icons/HomeIcon.vue'
import MovieIcon from '@/components/icons/MovieIcon.vue'
import ShowIcon from '@/components/icons/ShowIcon.vue'
import SettingIcon from '@/components/icons/SettingIcon.vue'
import CloseIcon from '@/components/icons/CloseIcon.vue'

const instanceStore = useInstanceStore()
const router = useRouter()

const isOpen = computed({
    get: () => instanceStore.getIsOpen,
    set: (value) => instanceStore.setIsOpen(value)
})

function navigate(route: string) {
    isOpen.value = !isOpen.value
    router.push({ name: route })
}
</script>
