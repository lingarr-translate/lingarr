<template>
    <div class="relative h-full w-full">
        <div v-if="loading" class="flex h-full w-full items-center justify-center">
            <LoaderCircleIcon class="h-8 w-8 animate-spin" />
        </div>
        <Bar
            v-else-if="chartData"
            :data="chartData"
            :options="chartOptions"
            class="h-full w-full" />
        <div v-else class="flex h-full w-full items-center justify-center text-primary-content">
            {{ translate('statistics.noDataAvailable') }}
        </div>
    </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import type { ChartData, ChartOptions, ChartDataset } from 'chart.js'
import {
    Chart as ChartJS,
    CategoryScale,
    LinearScale,
    BarElement,
    PointElement,
    LineElement,
    Title,
    Tooltip,
    Legend
} from 'chart.js'
import { Bar } from 'vue-chartjs'
import LoaderCircleIcon from '@/components/icons/LoaderCircleIcon.vue'
import services from '@/services'

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

interface DailyStatistic {
    date: string
    translationCount: number
}

const loading = ref(false)
const dailyStats = ref<DailyStatistic[]>([])

const fetchDailyStats = async () => {
    loading.value = true
    try {
        dailyStats.value = await services.statistics.getDailyStatistics<DailyStatistic[]>()
    } catch (error) {
        console.error('Error fetching daily statistics:', error)
    } finally {
        loading.value = false
    }
}

const calculateMovingAverage = (data: number[], windowSize: number): number[] => {
    return data.map((_, index) => {
        const start = Math.max(0, index - windowSize + 1)
        const slice = data.slice(start, index + 1)
        return Math.round(slice.reduce((sum, val) => sum + val, 0) / slice.length)
    })
}

type Dataset = ChartDataset<'bar' | 'line', number[]>

const chartData = computed(() => {
    if (!dailyStats.value.length) {
        return {
            labels: [],
            datasets: []
        }
    }

    // dates axis
    const dates = dailyStats.value.map((stat) =>
        new Date(stat.date).toLocaleDateString('en-US', { month: 'short', day: 'numeric' })
    )
    // translation axis
    const translationCounts = dailyStats.value.map((stat) => stat.translationCount)

    const movingAverage = calculateMovingAverage(translationCounts, 7)
    const datasets: Dataset[] = [
        {
            label: 'Daily Translations',
            data: translationCounts,
            backgroundColor: '#466e8c',
            barThickness: 6,
            borderRadius: 4,
            maxBarThickness: 12,
            order: 2
        },
        {
            label: '7-Day Average',
            data: movingAverage,
            backgroundColor: '#68d391',
            borderColor: '#68d391',
            borderWidth: 2,
            pointRadius: 0,
            fill: false,
            tension: 0.4,
            type: 'line',
            order: 1
        }
    ]

    return {
        labels: dates,
        datasets
    } as ChartData<'bar'>
})

const chartOptions: ChartOptions<'bar'> = {
    responsive: true,
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
                color: '#466e8c20'
            },
            ticks: {
                color: '#c0c8d2',
                font: {
                    size: 11
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
                color: '#c0c8d2',
                font: {
                    size: 11
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
                color: '#c0c8d2',
                font: {
                    size: 12
                },
                padding: 20,
                usePointStyle: true,
                pointStyle: 'circle'
            }
        },
        tooltip: {
            backgroundColor: '#1a202c',
            borderColor: '#466e8c',
            borderWidth: 1,
            padding: 12,
            titleColor: '#c0c8d2',
            bodyColor: '#c0c8d2',
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
}

onMounted(() => {
    fetchDailyStats()
})
</script>
