﻿<template>
    <div
        class="inline-flex items-center rounded-full border px-3 py-1 text-xs font-semibold whitespace-nowrap"
        :class="classes">
        <span><slot></slot></span>
    </div>
</template>

<script setup lang="ts">
const { classes = 'cursor-pointer text-primary-content border-accent bg-secondary' } = defineProps<{
    classes?: string
}>()
</script>
