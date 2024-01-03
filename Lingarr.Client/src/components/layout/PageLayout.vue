<template>
    <div class="min-h-screen">
        <HeaderLayout />
        <div class="flex grow">
            <MenuLayout />
            <div
                class="m-4 flex w-full flex-1 flex-wrap rounded-xl bg-white px-4 py-2 shadow-sm dark:bg-neutral-800">
                <slot></slot>
            </div>
        </div>
    </div>
</template>

<script setup lang="ts">
import HeaderLayout from '@/components/layout/HeaderLayout.vue'
import MenuLayout from '@/components/layout/MenuLayout.vue'
</script>
