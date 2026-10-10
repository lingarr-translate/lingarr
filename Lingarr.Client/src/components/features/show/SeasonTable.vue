<template>
    <div class="ui-border bg-secondary rounded-xl border p-2 sm:p-4">
        <div
            class="border-secondary bg-primary text-secondary-content grid grid-cols-12 border-b-2 font-bold">
            <div class="col-span-6 px-2 py-2 sm:px-4 md:col-span-3">Season</div>
            <div class="col-span-6 flex justify-between px-2 py-2 sm:px-4 md:col-span-8">
                <span>Episodes</span>
                <span class="hidden md:block">Include</span>
                <span class="block md:hidden">✓</span>
            </div>
        </div>
        <!-- Seasons -->
        <div
            v-for="season in seasons"
            :key="season.id"
            class="bg-primary text-primary-content text-sm md:text-base">
            <div
                class="grid grid-cols-12 transition-colors"
                :class="{ 'hover:bg-accent/5 cursor-pointer': season.episodes.length }"
                @click="toggleSeason(season)">
                <div
                    class="col-span-6 flex items-center px-2 py-2 select-none sm:px-4 md:col-span-3">
                    <CaretButton
                        v-if="season.episodes.length"
                        :is-expanded="expandedSeason?.id !== season.id"
                        class="pr-2" />
                    <div v-else class="w-7" />
                    <span v-if="season.seasonNumber == 0">Specials</span>
                    <span v-else>Season {{ season.seasonNumber }}</span>
                </div>
                <div
                    class="col-span-6 flex items-center justify-between px-2 py-2 select-none sm:px-4 md:col-span-8">
                    <span>
                        {{ season.episodes.length }}
                        episodes
                    </span>
                    <span @click.stop>
                        <ToggleButton
                            v-model="season.includeInTranslation"
                            :aria-label="`Include season ${season.seasonNumber} in automatic translation`"
                            size="small"
                            @toggle:update="
                                () =>
                                    showStore.include(
                                        MEDIA_TYPE.SEASON,
                                        season.id,
                                        season.includeInTranslation
                                    )
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
