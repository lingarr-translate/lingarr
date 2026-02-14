<template>
    <div class="rounded-lg bg-primary p-4 shadow-sm">
        <h3 class="text-primary-content/70 text-sm font-medium">
            {{ title }}
        </h3>
        <p class="mt-2 text-2xl font-bold text-primary-content">
            {{ formatNumber(value) }}
        </p>
    </div>
</template>

<script setup lang="ts">
interface Props {
    title: string
    value: number
}

withDefaults(defineProps<Props>(), {
    value: 0
})

const formatNumber = (num: number): string => {
    return new Intl.NumberFormat().format(num)
}
</script>
