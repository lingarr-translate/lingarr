<template>
    <div class="rounded-lg bg-primary p-4 shadow">
        <h3 class="text-sm font-medium text-primary-content/70">
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
