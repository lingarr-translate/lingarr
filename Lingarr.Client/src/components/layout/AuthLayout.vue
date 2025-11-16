<template>
    <div
        :class="[`${instanceStore.getTheme}`, 'bg-primary text-primary-content flex min-h-screen']">
        <div class="flex w-full flex-col drop-shadow-xl">
            <!-- Main Content -->
            <main class="flex-1">
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
import { useInstanceStore } from '@/store/instance'
const instanceStore = useInstanceStore()
</script>
