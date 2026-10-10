<template>
    <div class="ui-panel stat-card min-w-0 p-5 sm:p-6" :data-tone="tone">
        <div class="flex items-center gap-3">
            <span class="text-secondary-content" aria-hidden="true"><slot name="icon"></slot></span>
            <h3 class="text-secondary-content text-sm font-medium">{{ title }}</h3>
        </div>
        <div class="my-5 flex flex-wrap items-end justify-between gap-x-5 gap-y-2">
            <p class="text-4xl leading-none font-semibold tracking-tight tabular-nums">
                {{ formatNumber(total) }}
            </p>
            <p class="text-secondary-content text-sm">
                <span class="text-primary-content font-semibold tabular-nums">
                    {{ formatNumber(translated) }}
                </span>
                translated
            </p>
        </div>
        <div class="bg-primary h-1 w-full overflow-hidden rounded-full">
            <div
                class="stat-progress h-full rounded-full transition-all duration-500"
                :style="{ width: `${calculatePercentage(translated, total)}%` }"></div>
        </div>
    </div>
</template>

<script setup lang="ts">
interface Props {
    title: string
    tone?: string
    total: number
    translated: number
}

withDefaults(defineProps<Props>(), {
    tone: 'blue',
    total: 0,
    translated: 0
})

const formatNumber = (num: number): string => {
    return new Intl.NumberFormat().format(num)
}

const calculatePercentage = (value: number, total: number): number => {
    if (total === 0) return 0
    const percentage = (value / total) * 100
    return Math.min(percentage, 100)
}
</script>
