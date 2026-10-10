<template>
    <div class="relative h-full w-full">
        <Bar
            v-if="chartData"
            :key="key"
            aria-label="Daily translations and seven-day moving average"
            :data="chartData"
            :options="chartOptions"
            class="h-full w-full" />
        <div v-else class="text-primary-content flex h-full w-full items-center justify-center">
            No data available
        </div>
    </div>
</template>

<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import type { ChartData, ChartOptions } from 'chart.js'
import {
    Chart as ChartJS,
    ChartDataset,
    CategoryScale,
    LinearScale,
    BarElement,
    PointElement,
    LineElement,
    Title,
    Tooltip,
    Legend
} from 'chart.js/auto'
import { Bar } from 'vue-chartjs'
import { DailyStatistic } from '@/ts'
import { formatDayLabel } from '@/utils/date'
import { useInstanceStore } from '@/store/instance'

ChartJS.register(
    CategoryScale,
    LinearScale,
    BarElement,
    PointElement,
    LineElement,
    Title,
    Tooltip,
    Legend
)

const props = defineProps<{
    dailyStats: DailyStatistic[]
}>()

const getCssVariable = (variableName: string): string => {
    return getComputedStyle(document.documentElement).getPropertyValue(variableName).trim()
}

const colors = computed(() => {
    return {
        key: key.value, // force reload
        bar: {
            background: getCssVariable('--accent'),
            border: getCssVariable('--primary')
        },
        line: {
            border: getCssVariable('--primary-content')
        },
        card: {
            background: getCssVariable('--primary'),
            border: getCssVariable('--accent')
        }
    }
})
const instanceStore = useInstanceStore()
const key = ref(0)
watch(
    () => instanceStore.getTheme,
    async () => {
        key.value++
    }
)

const calculateMovingAverage = (data: number[], windowSize: number): number[] => {
    return data.map((_, index) => {
        const start = Math.max(0, index - windowSize + 1)
        const slice = data.slice(start, index + 1)
        return Math.round(slice.reduce((sum, val) => sum + val, 0) / slice.length)
    })
}

const dates = computed(() => props.dailyStats.map((stat) => formatDayLabel(stat.date)))

const translationCounts = computed(() => props.dailyStats.map((stat) => stat.translationCount))
const movingAverage = computed(() => calculateMovingAverage(translationCounts.value, 7))

const chartData = computed<ChartData<'bar'> | undefined>(() => {
    if (!props.dailyStats.length) return undefined

    const datasets: ChartDataset<'bar' | 'line'>[] = [
        {
            label: 'Daily Translations',
            data: translationCounts.value,
            backgroundColor: colors.value.bar.background,
            borderColor: colors.value.bar.border,
            barThickness: 8,
            borderRadius: 4,
            maxBarThickness: 12,
            order: 2
        },
        {
            label: '7-Day Average',
            data: movingAverage.value,
            borderColor: colors.value.line.border,
            borderWidth: 2,
            pointRadius: 0,
            fill: false,
            tension: 0.4,
            type: 'line',
            order: 1
        }
    ]

    return {
        labels: dates.value,
        datasets
    } as ChartData<'bar'>
})

const chartOptions = computed<ChartOptions<'bar'>>(() => ({
    responsive: true,
    animation: window.matchMedia('(prefers-reduced-motion: reduce)').matches ? false : undefined,
    maintainAspectRatio: false,
    layout: {
        padding: {
            top: 20
        }
    },
    scales: {
        y: {
            beginAtZero: true,
            grid: {
                color: getCssVariable('--secondary-content') + '20'
            },
            ticks: {
                color: getCssVariable('--secondary-content'),
                font: {
                    size: 12
                },
                padding: 8,
                maxTicksLimit: 5
            },
            border: {
                display: false
            }
        },
        x: {
            grid: {
                display: false
            },
            ticks: {
                color: getCssVariable('--secondary-content'),
                font: {
                    size: 12
                },
                maxRotation: 45,
                minRotation: 45,
                padding: 8,
                maxTicksLimit: 10
            },
            border: {
                display: false
            }
        }
    },
    plugins: {
        legend: {
            position: 'top' as const,
            labels: {
                color: getCssVariable('--secondary-content'),
                font: {
                    size: 12
                },
                padding: 20,
                usePointStyle: true,
                pointStyle: 'circle'
            }
        },
        tooltip: {
            backgroundColor: colors.value.card.background,
            borderColor: colors.value.card.border,
            borderWidth: 1,
            padding: 12,
            titleColor: getCssVariable('--primary-content'),
            bodyColor: getCssVariable('--primary-content'),
            titleFont: {
                size: 13
            },
            bodyFont: {
                size: 12
            },
            displayColors: false,
            callbacks: {
                title(tooltipItems) {
                    return tooltipItems[0].label
                },
                label(context) {
                    if (context.datasetIndex === 1) {
                        return `Average: ${context.parsed.y} translations`
                    }
                    return `${context.parsed.y} translations`
                }
            }
        }
    },
    interaction: {
        intersect: false,
        mode: 'index'
    }
}))
</script>
