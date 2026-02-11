<template>
    <div class="grid grid-cols-1 gap-4 lg:grid-cols-2">
        <CardComponent title="Media Overview" class="lg:col-span-2">
            <template #content>
                <template v-if="loading">
                    <div class="flex h-64 items-center justify-center">
                        <LoaderCircleIcon class="h-8 w-8 animate-spin" />
                    </div>
                </template>

                <template v-else-if="error">
                    <div class="flex h-64 items-center justify-center text-red-500">
                        {{ error }}
                    </div>
                </template>

                <template v-else-if="!statistics">
                    <div class="flex h-64 items-center justify-center text-primary-content">
                        No statistics available
                    </div>
                </template>

                <template v-else>
                    <div class="grid grid-cols-1 gap-4 md:grid-cols-2">
                        <StatCard
                            title="Movies"
                            :total="statistics.totalMovies"
                            :translated="getTranslationCount(MEDIA_TYPE.MOVIE)" />

                        <StatCard
                            title="TV Shows"
                            :total="statistics.totalEpisodes"
                            :translated="getTranslationCount(MEDIA_TYPE.EPISODE)" />
                    </div>
                </template>
            </template>
        </CardComponent>

        <CardComponent title="Translation Activity">
            <template #content>
                <div class="grid grid-cols-1 gap-4 md:grid-cols-2 lg:grid-cols-1 xl:grid-cols-2">
                    <MetricCard
                        title="Lines Translated"
                        :value="statistics?.totalLinesTranslated ?? 0" />

                    <MetricCard
                        title="Files Processed"
                        :value="statistics?.totalFilesTranslated ?? 0" />

                    <MetricCard
                        title="Characters Translated"
                        :value="statistics?.totalCharactersTranslated ?? 0"
                        class="xl:col-span-2" />
                </div>

                <div v-if="translationServices.length" class="mt-4">
                    <h3 class="mb-4 text-sm font-medium text-primary-content">
                        Translation Services
                    </h3>
                    <div class="grid grid-cols-2 gap-2 xl:grid-cols-3">
                        <div
                            v-for="[service, count] in translationServices"
                            :key="service"
                            class="rounded-sm bg-primary p-2">
                            <h4 class="text-primary-content/70 text-xs font-medium">
                                {{ formatServiceName(service) }}
                            </h4>
                            <p class="text-lg font-bold text-primary-content">
                                {{ formatNumber(count) }}
                            </p>
                        </div>
                    </div>
                </div>
            </template>
        </CardComponent>

        <CardComponent title="Language Statistics">
            <template #content>
                <div class="h-80">
                    <LanguageChart v-if="dailyStats?.length" :daily-stats="dailyStats" />
                    <div v-else-if="loading" class="flex h-full w-full items-center justify-center">
                        <LoaderCircleIcon class="h-8 w-8 animate-spin" />
                    </div>
                    <div v-else class="flex h-full w-full items-center justify-center text-xs">
                        No statistics available.
                    </div>
                </div>

                <div v-if="subtitleLanguages.length" class="mt-4">
                    <h3 class="mb-2 text-sm font-medium text-primary-content">
                        Available Subtitles
                    </h3>
                    <div class="grid grid-cols-3 gap-2 md:grid-cols-4 xl:grid-cols-6">
                        <div
                            v-for="[language, count] in subtitleLanguages"
                            :key="language"
                            class="rounded-sm bg-primary p-2">
                            <h4 class="text-primary-content/70 text-xs font-medium">
                                {{ language.toUpperCase() }}
                            </h4>
                            <p class="text-lg font-bold text-primary-content">
                                {{ formatNumber(count) }}
                            </p>
                        </div>
                    </div>
                </div>

                <div class="flex justify-end">
                    <ButtonComponent
                        variant="ghost"
                        size="xs"
                        :disabled="loading || resetting"
                        :loading="resetting"
                        @click="handleResetStatistics">
                        Reset statistics
                    </ButtonComponent>
                </div>
            </template>
        </CardComponent>
    </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { DailyStatistic, MEDIA_TYPE, Statistics } from '@/ts'
import services from '@/services'
import CardComponent from '@/components/common/CardComponent.vue'
import ButtonComponent from '@/components/common/ButtonComponent.vue'
import LoaderCircleIcon from '@/components/icons/LoaderCircleIcon.vue'
import LanguageChart from './LanguageChart.vue'
import StatCard from './StatCard.vue'
import MetricCard from './MetricCard.vue'

const loading = ref(true)
const resetting = ref(false)
const error = ref<string | null>(null)
const statistics = ref<Statistics>()
const dailyStats = ref<DailyStatistic[]>()

const translationServices = computed(() => {
    if (!statistics.value?.translationsByService) return []
    return Object.entries(statistics.value.translationsByService)
})

const subtitleLanguages = computed(() => {
    if (!statistics.value?.subtitlesByLanguage) return []
    return Object.entries(statistics.value.subtitlesByLanguage)
})

const formatNumber = (num: number): string => {
    return num ? new Intl.NumberFormat().format(num) : '0'
}

const formatServiceName = (service: string): string => {
    return service.charAt(0).toUpperCase() + service.slice(1)
}

const getTranslationCount = (type: string): number => {
    return statistics.value?.translationsByMediaType?.[type] || 0
}

const fetchStatistics = async () => {
    try {
        error.value = null
        loading.value = true
        statistics.value = await services.statistics.getStatistics()
    } catch (err: unknown) {
        if (err instanceof Error) {
            error.value = err?.message || 'Failed to fetch statistics'
        }
        console.error('Error fetching statistics:', err)
    } finally {
        loading.value = false
    }
}

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

const handleResetStatistics = async () => {
    if (!confirm('Are you sure you want to reset all statistics? This action cannot be undone.')) {
        return
    }

    try {
        resetting.value = true
        error.value = null
        await services.statistics.resetStatistics()
        await fetchDailyStats()
        await fetchStatistics()
    } catch (err: unknown) {
        if (err instanceof Error) {
            error.value = err?.message || 'Failed to reset statistics'
        }
        console.error('Error resetting statistics:', err)
    } finally {
        resetting.value = false
    }
}

onMounted(async () => {
    await fetchDailyStats()
    await fetchStatistics()
})
</script>
