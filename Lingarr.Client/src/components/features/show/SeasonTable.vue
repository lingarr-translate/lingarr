<template>
    <div class="bg-secondary p-4">
        <div
            class="border-secondary bg-primary text-secondary-content grid grid-cols-12 border-b-2 font-bold">
            <div class="col-span-6 px-4 py-2 md:col-span-3">
                {{ translate('tvShows.season') }}
            </div>
            <div class="col-span-6 flex justify-between px-4 py-2 md:col-span-8">
                <span>{{ translate('tvShows.episodes') }}</span>
                <span class="hidden md:block">
                    {{ translate('tvShows.exclude') }}
                </span>
                <span class="block md:hidden">⊘</span>
            </div>
        </div>
        <!-- Seasons -->
        <div
            v-for="season in seasons"
            :key="season.id"
            class="bg-primary text-accent-content text-sm md:text-base">
            <div
                class="grid grid-cols-12"
                :class="{ 'cursor-pointer': season.episodes.length }"
                @click="toggleSeason(season)">
                <div class="col-span-6 flex items-center px-4 py-2 select-none md:col-span-3">
                    <CaretButton
                        v-if="season.episodes.length"
                        :is-expanded="expandedSeason?.id !== season.id"
                        class="pr-2" />
                    <div v-else class="w-7" />
                    <span v-if="season.seasonNumber == 0">
                        {{ translate('tvShows.specials') }}
                    </span>
                    <span v-else>{{ translate('tvShows.season') }} {{ season.seasonNumber }}</span>
                </div>
                <div class="col-span-6 flex justify-between px-4 py-2 select-none md:col-span-8">
                    <span>
                        {{ season.episodes.length }}
                        {{ translate('tvShows.episodesLine') }}
                    </span>
                    <span @click.stop>
                        <ToggleButton
                            v-model="season.excludeFromTranslation"
                            size="small"
                            @toggle:update="
                                () => showStore.exclude(MEDIA_TYPE.SEASON, season.id)
                            " />
                    </span>
                </div>
            </div>
            <EpisodeTable
                v-if="expandedSeason?.id === season.id"
                :subtitles="subtitles"
                :episodes="season.episodes" />
        </div>
    </div>
</template>
<script setup lang="ts">
import { ref, Ref } from 'vue'
import { ISeason, ISubtitle, MEDIA_TYPE } from '@/ts'
import EpisodeTable from '@/components/features/show/EpisodeTable.vue'
import CaretButton from '@/components/common/CaretButton.vue'
import ToggleButton from '@/components/common/ToggleButton.vue'
import services from '@/services'
import { useShowStore } from '@/store/show'

defineProps<{
    seasons: ISeason[]
}>()

const showStore = useShowStore()
const subtitles: Ref<ISubtitle[]> = ref([])
const expandedSeason: Ref<ISeason | null> = ref(null)

async function toggleSeason(season: ISeason) {
    if (!season.episodes.length) return
    if (expandedSeason.value?.id === season.id) {
        expandedSeason.value = null
        return
    }
    expandedSeason.value = season
    await collectSubtitles()
}

async function collectSubtitles() {
    if (expandedSeason.value?.path) {
        subtitles.value = await services.subtitle.collect(expandedSeason.value.path)
    }
}
</script>
