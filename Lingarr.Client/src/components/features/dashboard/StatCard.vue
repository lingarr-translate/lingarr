<template>
    <div class="bg-primary rounded-lg p-4 shadow-sm">
        <div class="flex justify-between">
            <div>
                <h3 class="text-primary-content/70 text-sm font-medium">
                    {{ title }}
                </h3>
                <p class="text-primary-content mt-2 text-2xl font-bold">
                    {{ formatNumber(total) }}
                </p>
            </div>
            <div class="text-right">
                <h3 class="text-primary-content/70 text-sm font-medium">
                    {{ translate('statistics.translated') }}
                </h3>
                <p class="text-accent mt-2 text-xl font-bold">
                    {{ formatNumber(translated) }}
                </p>
            </div>
        </div>
        <div class="bg-secondary mt-2 h-2 w-full overflow-hidden rounded-full">
            <div
                class="bg-accent h-full rounded-full transition-all duration-500"
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
    if (total === 0) return 0
    const percentage = (value / total) * 100
    return Math.min(percentage, 100)
}
</script>
