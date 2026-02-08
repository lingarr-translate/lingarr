<template>
    <div class="bg-secondary p-4">
        <div
            class="border-secondary bg-primary text-secondary-content grid grid-cols-12 border-b-2 font-bold">
            <div class="col-span-6 px-4 py-2 md:col-span-3">
                {{ translate('tvShows.season') }}
            </div>
            <div
                :class="isSelectMode ? 'md:col-span-7' : 'md:col-span-8'"
                class="col-span-6 flex justify-between px-4 py-2">
                <span>{{ translate('tvShows.episodes') }}</span>
                <span class="hidden md:block">
                    {{ translate('tvShows.exclude') }}
                </span>
                <span class="block md:hidden">⊘</span>
            </div>
            <div v-if="isSelectMode" class="col-span-1 flex items-center justify-center px-4 py-2">
                <CheckboxComponent :model-value="allSeasonsSelected" @change="toggleAllSeasons" />
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
                <div
                    :class="isSelectMode ? 'md:col-span-7' : 'md:col-span-8'"
                    class="col-span-6 flex justify-between px-4 py-2 select-none">
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
                <div
                    v-if="isSelectMode"
                    class="col-span-1 flex items-center justify-center px-4 py-2"
                    @click.stop>
                    <CheckboxComponent
                        :model-value="!!showStore.selectedSeasons[season.id]"
                        @change="showStore.toggleSeasonSelect(season, show)" />
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
import { ref, Ref, computed } from 'vue'
import { ISeason, IShow, ISubtitle, MEDIA_TYPE } from '@/ts'
import EpisodeTable from '@/components/features/show/EpisodeTable.vue'
import CaretButton from '@/components/common/CaretButton.vue'
import ToggleButton from '@/components/common/ToggleButton.vue'
import CheckboxComponent from '@/components/common/CheckboxComponent.vue'
import services from '@/services'
import { useShowStore } from '@/store/show'

const props = defineProps<{
    seasons: ISeason[]
    show: IShow
    isSelectMode: boolean
}>()

const showStore = useShowStore()

const allSeasonsSelected = computed(() =>
    props.seasons.every((s) => showStore.selectedSeasons[s.id])
)

function toggleAllSeasons() {
    if (allSeasonsSelected.value) {
        for (const season of props.seasons) {
            if (showStore.selectedSeasons[season.id]) {
                showStore.toggleSeasonSelect(season, props.show)
            }
        }
    } else {
        for (const season of props.seasons) {
            if (!showStore.selectedSeasons[season.id]) {
                showStore.toggleSeasonSelect(season, props.show)
            }
        }
    }
}
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
