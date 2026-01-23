<template>
    <div class="bg-secondary p-4">
        <div
            class="grid grid-cols-12 border-b-2 border-secondary bg-primary font-bold text-secondary-content">
            <div class="col-span-6 px-4 py-2 md:col-span-3">Season</div>
            <div class="col-span-6 flex justify-between px-4 py-2 md:col-span-8">
                <span>Episodes</span>
                <span class="hidden md:block">Exclude</span>
                <span class="block md:hidden">⊘</span>
            </div>
        </div>
        <!-- Seasons -->
        <div
            v-for="season in seasons"
            :key="season.id"
            class="bg-primary text-sm text-accent-content md:text-base">
            <div
                class="grid grid-cols-12"
                :class="{ 'cursor-pointer': season.episodes.length }"
                @click="toggleSeason(season)">
                <div class="col-span-6 flex select-none items-center px-4 py-2 md:col-span-3">
                    <CaretButton
                        v-if="season.episodes.length"
                        :is-expanded="expandedSeason?.id !== season.id"
                        class="pr-2" />
                    <div v-else class="w-7" />
                    <span v-if="season.seasonNumber == 0">Specials</span>
                    <span v-else>Season {{ season.seasonNumber }}</span>
                </div>
                <div class="col-span-6 flex select-none justify-between px-4 py-2 md:col-span-8">
                    <span>
                        {{ season.episodes.length }}
                        episodes
                    </span>
                    <span @click.stop>
                        <ToggleButton
                            :model-value="!season.excludeFromTranslation"
                            size="small"
                            @toggle:update="() => handleIncludeToggle(season)" />
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

async function handleIncludeToggle(season: ISeason) {
    const currentlyIncluded = !season.excludeFromTranslation
    const newIncludeState = !currentlyIncluded
    await showStore.include(MEDIA_TYPE.SEASON, season.id, newIncludeState)
}
</script>
