<template>
    <div class="flex h-screen flex-col bg-blue-300/20 dark:bg-neutral-900 dark:text-neutral-300">
        <LoadingIndicator />
        <main
            class="h-full transform overflow-y-auto overflow-x-hidden transition-transform duration-200 ease-in-out">
            <router-view></router-view>
        </main>
        <StatusNotification />
    </div>
</template>

<script setup lang="ts">
import LoadingIndicator from '@/components/LoadingIndicator.vue'
import StatusNotification from '@/components/StatusNotification.vue'
</script>
