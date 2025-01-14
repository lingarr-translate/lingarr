<template>
    <div class="rounded-lg bg-primary p-4 shadow">
        <div class="flex justify-between">
            <div>
                <h3 class="text-sm font-medium text-primary-content/70">
                    {{ title }}
                </h3>
                <p class="mt-2 text-2xl font-bold text-primary-content">
                    {{ formatNumber(total) }}
                </p>
            </div>
            <div class="text-right">
                <h3 class="text-sm font-medium text-primary-content/70">
                    {{ translate('statistics.translated') }}
                </h3>
                <p class="mt-2 text-xl font-bold text-accent">
                    {{ formatNumber(translated) }}
                </p>
            </div>
        </div>
        <div class="mt-2 h-2 w-full rounded-full bg-secondary">
            <div
                class="h-full rounded-full bg-accent transition-all duration-500"
                :style="{ width: `${calculatePercentage(translated, total)}%` }"></div>
        </div>
    </div>
</template>

<script setup lang="ts">
interface Props {
    title: string
    total: number
    translated: number
}

withDefaults(defineProps<Props>(), {
    total: 0,
    translated: 0
})

const formatNumber = (num: number): string => {
    return new Intl.NumberFormat().format(num)
}

const calculatePercentage = (value: number, total: number): number => {
    return (value / total) * 100
}
</script>
