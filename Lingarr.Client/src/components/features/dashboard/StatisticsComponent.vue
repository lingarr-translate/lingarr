<template>
    <div class="grid grid-cols-1 gap-4 lg:grid-cols-2">
        <CardComponent :title="translate('statistics.mediaOverview')" class="lg:col-span-2">
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
                    <div class="text-primary-content flex h-64 items-center justify-center">
                        {{ translate('statistics.notAvailable') }}
                    </div>
                </template>

                <template v-else>
                    <div class="grid grid-cols-1 gap-4 md:grid-cols-2">
                        <StatCard
                            :title="translate('statistics.movies')"
                            :total="statistics.totalMovies"
                            :translated="getTranslationCount(MEDIA_TYPE.MOVIE)" />

                        <StatCard
                            :title="translate('statistics.tvShows')"
                            :total="statistics.totalEpisodes"
                            :translated="getTranslationCount(MEDIA_TYPE.EPISODE)" />
                    </div>
                </template>
            </template>
        </CardComponent>

        <CardComponent :title="translate('statistics.translationActivity')">
            <template #content>
                <div class="grid grid-cols-1 gap-4 md:grid-cols-2 lg:grid-cols-1 xl:grid-cols-2">
                    <MetricCard
                        :title="translate('statistics.linesTranslated')"
                        :value="statistics?.totalLinesTranslated ?? 0" />

                    <MetricCard
                        :title="translate('statistics.filesProcessed')"
                        :value="statistics?.totalFilesTranslated ?? 0" />

                    <MetricCard
                        :title="translate('statistics.charactersTranslated')"
                        :value="statistics?.totalCharactersTranslated ?? 0"
                        class="xl:col-span-2" />
                </div>

                <div v-if="translationServices.length" class="mt-4">
                    <h3 class="text-primary-content mb-4 text-sm font-medium">
                        {{ translate('statistics.translationServices') }}
                    </h3>
                    <div class="grid grid-cols-2 gap-2 xl:grid-cols-3">
                        <div
                            v-for="[service, count] in translationServices"
                            :key="service"
                            class="bg-primary rounded-sm p-2">
                            <h4 class="text-primary-content/70 text-xs font-medium">
                                {{ formatServiceName(service) }}
                            </h4>
                            <p class="text-primary-content text-lg font-bold">
                                {{ formatNumber(count) }}
                            </p>
                        </div>
                    </div>
                </div>
            </template>
        </CardComponent>

        <CardComponent :title="translate('statistics.languageStatistics')">
            <template #content>
                <div class="h-80">
                    <LanguageChart v-if="dailyStats?.length" :daily-stats="dailyStats" />
                    <div v-else class="flex h-full w-full items-center justify-center">
                        <LoaderCircleIcon class="h-8 w-8 animate-spin" />
                    </div>
                </div>

                <div v-if="subtitleLanguages.length" class="mt-4">
                    <h3 class="text-primary-content mb-2 text-sm font-medium">
                        {{ translate('statistics.availableSubtitles') }}
                    </h3>
                    <div class="grid grid-cols-3 gap-2 md:grid-cols-4 xl:grid-cols-6">
                        <div
                            v-for="[language, count] in subtitleLanguages"
                            :key="language"
                            class="bg-primary rounded-sm p-2">
                            <h4 class="text-primary-content/70 text-xs font-medium">
                                {{ language.toUpperCase() }}
                            </h4>
                            <p class="text-primary-content text-lg font-bold">
                                {{ formatNumber(count) }}
                            </p>
                        </div>
                    </div>
                </div>
            </template>
        </CardComponent>
    </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { DailyStatistic, MEDIA_TYPE, Statistics } from '@/ts'
import { useI18n } from '@/plugins/i18n'
import services from '@/services'
import CardComponent from '@/components/common/CardComponent.vue'
import LoaderCircleIcon from '@/components/icons/LoaderCircleIcon.vue'
import LanguageChart from './LanguageChart.vue'
import StatCard from './StatCard.vue'
import MetricCard from './MetricCard.vue'
const { translate } = useI18n()

const loading = ref(true)
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
            error.value = err?.message || translate('statistics.failedFetch')
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

onMounted(async () => {
    await fetchDailyStats()
    await fetchStatistics()
})
</script>
