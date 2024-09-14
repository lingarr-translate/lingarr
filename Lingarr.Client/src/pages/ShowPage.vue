<template>
    <PageLayout>
        <main>
            <!-- Search and Filters -->
            <div class="flex flex-wrap items-center justify-between gap-2 bg-tertiary p-4">
                <div class="relative flex-grow">
                    <SearchComponent v-model="filter" />
                </div>
                <div class="flex space-x-2">
                    <SortControls
                        v-model="filter"
                        :options="[
                            {
                                label: 'Sort by Title',
                                value: 'Title'
                            },
                            {
                                label: 'Sort by Date Added',
                                value: 'DateAdded'
                            }
                        ]" />
                </div>
            </div>

            <!-- Media List View -->
            <div class="w-full px-4">
                <div class="grid grid-cols-12 border-b border-accent font-bold">
                    <div class="col-span-1 px-4 py-2"></div>
                    <div class="col-span-5 px-4 py-2">Title</div>
                    <div class="col-span-4 px-4 py-2">Subtitles</div>
                    <div class="col-span-2 px-4 py-2"></div>
                </div>
                <div v-for="item in shows.items" :key="item.id">
                    <div
                        class="grid cursor-pointer grid-cols-12 border-b border-accent"
                        @click="toggleShow(item)">
                        <div class="col-span-1 px-4 py-2">
                            <ToggleButton :is-expanded="expandedShow !== item.id" />
                        </div>
                        <div class="col-span-11 px-4 py-2">
                            {{ item.title }}
                        </div>
                    </div>
                    <div v-if="expandedShow === item.id" class="bg-secondary p-4">
                        <div
                            class="grid grid-cols-12 border-b-2 border-secondary bg-primary font-bold text-secondary-content">
                            <div class="col-span-1 px-4 py-2"></div>
                            <div class="col-span-3 px-4 py-2">Season</div>
                            <div class="col-span-8 px-4 py-2">Episodes</div>
                        </div>
                        <div
                            v-for="season in item.seasons"
                            :key="season.id"
                            class="bg-primary text-accent-content">
                            <div
                                class="grid grid-cols-12"
                                :class="{ 'cursor-pointer': season.episodes.length }"
                                @click="toggleSeason(season)">
                                <div class="col-span-1 px-4 py-2">
                                    <ToggleButton
                                        v-if="season.episodes.length"
                                        :is-expanded="expandedSeason !== season.id" />
                                </div>
                                <div class="col-span-3 select-none px-4 py-2">
                                    Season {{ season.seasonNumber }}
                                </div>
                                <div class="col-span-8 select-none px-4 py-2">
                                    {{ season.episodes.length }} episodes
                                </div>
                            </div>
                            <div v-if="expandedSeason === season.id" class="bg-secondary">
                                <div
                                    class="grid grid-cols-12 border-b-2 border-primary bg-tertiary font-bold text-secondary-content">
                                    <div class="col-span-1 px-4 py-2"></div>
                                    <div class="col-span-2 px-4 py-2">Episode</div>
                                    <div class="col-span-5 px-4 py-2">Title</div>
                                    <div class="col-span-4 px-4 py-2">Subtitles</div>
                                </div>
                                <div
                                    v-for="(episode, index) in season.episodes"
                                    :key="episode.id"
                                    class="grid grid-cols-12 bg-tertiary text-tertiary-content">
                                    <div class="col-span-1 px-4 py-2"></div>
                                    <div class="col-span-2 px-4 py-2">{{ index + 1 }}</div>
                                    <div class="col-span-5 px-4 py-2">
                                        {{ episode.title }}
                                    </div>
                                    <div
                                        v-if="
                                            episode?.fileName &&
                                            getSubtitle(episode.fileName).length
                                        "
                                        class="col-span-2 flex items-center gap-2">
                                        <ContextMenu
                                            v-for="(subtitle, jndex) in getSubtitle(
                                                episode.fileName
                                            )"
                                            :key="`${episode.id}-${jndex}`"
                                            :subtitle="subtitle">
                                            <BadgeComponent value="{{ subtitle.language }}">
                                                {{ subtitle.language.toUpperCase() }}
                                            </BadgeComponent>
                                        </ContextMenu>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>

            <PaginationComponent
                v-if="shows.totalCount"
                v-model="filter"
                :total-count="shows.totalCount"
                :page-size="shows.pageSize" />
        </main>
    </PageLayout>
</template>

<script setup lang="ts">
import { ref, Ref, computed, onMounted, ComputedRef } from 'vue'
import { IFilter, IPagedResult, ISeason, IShow, ISubtitle } from '@/ts'

import { useInstanceStore } from '@/store/instance'
import { useShowStore } from '@/store/show'
import services from '@/services'
import PaginationComponent from '@/components/PaginationComponent.vue'
import PageLayout from '@/components/PageLayout.vue'
import SearchComponent from '@/components/SearchComponent.vue'
import ToggleButton from '@/components/ToggleButton.vue'
import SortControls from '@/components/SortControls.vue'
import BadgeComponent from '@/components/BadgeComponent.vue'
import ContextMenu from '@/components/ContextMenu.vue'

const instanceStore = useInstanceStore()
const showStore = useShowStore()

const expandedShow: Ref<boolean | number | null> = ref(null)
const expandedSeason: Ref<boolean | number | null> = ref(null)
const subtitles: Ref<ISubtitle[]> = ref([])

const shows: ComputedRef<IPagedResult<IShow>> = computed(() => showStore.get)
const filter: ComputedRef<IFilter> = computed({
    get: () => showStore.getFilter,
    set: (value: IFilter) => showStore.setFilter(value)
})

async function toggleShow(show: IShow) {
    if (expandedShow.value === show.id) {
        expandedShow.value = null
        return
    }
    instanceStore.setPoster({ content: show, type: 'show' })
    expandedShow.value = show.id
}

async function toggleSeason(season: ISeason) {
    if (!season.episodes.length) return
    if (expandedSeason.value === season.id) {
        expandedSeason.value = null
        return
    }
    subtitles.value = await services.subtitle.collect(season.path)
    expandedSeason.value = season.id
}

const getSubtitle = (fileName: string | null) => {
    return subtitles.value
        .filter((subtitle: ISubtitle) => subtitle.fileName === fileName)
        .slice()
        .sort((a, b) => a.language.localeCompare(b.language))
}

onMounted(() => {
    showStore.fetch()
})
</script>
