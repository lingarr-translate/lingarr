<template>
    <div class="space-y-8">
        <section aria-labelledby="media-overview-heading">
            <h2 id="media-overview-heading" class="mb-4 text-base font-semibold">Media overview</h2>
            <div
                v-if="loading"
                class="ui-panel flex min-h-44 items-center justify-center"
                role="status">
                <LoaderCircleIcon class="h-6 w-6 animate-spin" />
                <span class="sr-only">Loading statistics</span>
            </div>
            <div v-else-if="error" class="ui-panel p-6 text-[var(--danger)]" role="alert">
                {{ error }}
            </div>
            <div v-else-if="!statistics" class="ui-panel text-secondary-content p-6">
                No statistics available.
            </div>
            <div v-else class="grid grid-cols-1 gap-4 md:grid-cols-2">
                <StatCard
                    title="Movies"
                    :total="statistics.totalMovies"
                    :translated="getTranslationCount(MEDIA_TYPE.MOVIE)">
                    <template #icon><FeatureIcon kind="movies" class="h-10 w-10" /></template>
                </StatCard>
                <StatCard
                    title="TV episodes"
                    tone="violet"
                    :total="statistics.totalEpisodes"
                    :translated="getTranslationCount(MEDIA_TYPE.EPISODE)">
                    <template #icon><FeatureIcon kind="shows" class="h-10 w-10" /></template>
                </StatCard>
            </div>
        </section>

        <section aria-labelledby="translation-totals-heading">
            <h2 id="translation-totals-heading" class="mb-4 text-base font-semibold">
                Translation totals
            </h2>
            <div class="ui-panel dashboard-metrics grid grid-cols-1 sm:grid-cols-3">
                <MetricCard
                    kind="files"
                    title="Files processed"
                    :value="statistics?.totalFilesTranslated ?? 0" />
                <MetricCard
                    kind="lines"
                    title="Lines translated"
                    :value="statistics?.totalLinesTranslated ?? 0" />
                <MetricCard
                    kind="characters"
                    title="Characters translated"
                    :value="statistics?.totalCharactersTranslated ?? 0" />
            </div>
        </section>

        <div class="grid grid-cols-1 items-start gap-6 xl:grid-cols-3">
            <section
                aria-labelledby="translation-activity-heading"
                class="ui-panel min-w-0 p-5 sm:p-6 xl:col-span-2">
                <div class="mb-6 flex items-center gap-3">
                    <FeatureIcon kind="dashboard" class="h-10 w-10" />
                    <div>
                        <h2
                            id="translation-activity-heading"
                            class="text-lg font-semibold tracking-tight">
                            Translation activity
                        </h2>
                        <p class="text-secondary-content mt-1 text-sm">
                            Daily translations over the last 30 days
                        </p>
                    </div>
                </div>
                <div class="h-72 sm:h-80">
                    <LanguageChart v-if="dailyStats?.length" :daily-stats="dailyStats" />
                    <div
                        v-else-if="loading"
                        class="flex h-full items-center justify-center"
                        role="status">
                        <LoaderCircleIcon class="h-6 w-6 animate-spin" />
                        <span class="sr-only">Loading activity</span>
                    </div>
                    <p
                        v-else
                        class="text-secondary-content flex h-full items-center justify-center text-sm">
                        No translation activity yet.
                    </p>
                </div>
            </section>

            <div class="ui-panel min-w-0 p-5 sm:p-6">
                <section aria-labelledby="translation-services-heading">
                    <h2 id="translation-services-heading" class="text-base font-semibold">
                        Translation services
                    </h2>
                    <dl v-if="translationServices.length" class="mt-5 space-y-4">
                        <div
                            v-for="[service, count] in translationServices"
                            :key="service"
                            class="flex items-center justify-between gap-4">
                            <dt class="text-secondary-content text-sm">
                                {{ formatServiceName(service) }}
                            </dt>
                            <dd class="text-sm font-semibold tabular-nums">
                                {{ formatNumber(count) }}
                            </dd>
                        </div>
                    </dl>
                    <p v-else class="text-secondary-content mt-4 text-sm">No services used yet.</p>
                </section>
                <section
                    aria-labelledby="subtitle-languages-heading"
                    class="dashboard-divider mt-6 border-t pt-6">
                    <h2 id="subtitle-languages-heading" class="text-base font-semibold">
                        Available subtitles
                    </h2>
                    <dl v-if="subtitleLanguages.length" class="mt-5 space-y-4">
                        <div
                            v-for="[language, count] in subtitleLanguages"
                            :key="language"
                            class="flex items-center justify-between gap-4">
                            <dt class="text-secondary-content text-sm font-medium uppercase">
                                {{ language }}
                            </dt>
                            <dd class="text-sm font-semibold tabular-nums">
                                {{ formatNumber(count) }}
                            </dd>
                        </div>
                    </dl>
                    <p v-else class="text-secondary-content mt-4 text-sm">
                        No subtitles found yet.
                    </p>
                </section>
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
    </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { DailyStatistic, MEDIA_TYPE, Statistics } from '@/ts'
import services from '@/services'
import FeatureIcon from '@/components/common/FeatureIcon.vue'
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
