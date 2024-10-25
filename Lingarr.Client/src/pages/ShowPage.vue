<template>
    <PageLayout>
        <div v-if="settingsCompleted === 'true'" class="w-full">
            <!-- Search and Filters -->
            <div class="flex flex-wrap items-center justify-between gap-2 bg-tertiary p-4">
                <SearchComponent v-model="filter" />
                <div class="flex w-full justify-between space-x-2 md:w-fit">
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
                    <div class="col-span-2 flex justify-end px-4 py-2">
                        <ReloadComponent @toggle:update="showStore.fetch()" />
                    </div>
                </div>
                <div v-for="item in shows.items" :key="item.id">
                    <div
                        class="grid cursor-pointer grid-cols-12 border-b border-accent"
                        @click="toggleShow(item)">
                        <div class="col-span-1 px-2 py-2 md:px-4">
                            <CaretButton :is-expanded="expandedShow !== item.id" />
                        </div>
                        <div class="col-span-11 px-4 py-2">
                            {{ item.title }}
                        </div>
                    </div>
                    <div v-if="expandedShow === item.id" class="bg-secondary p-4">
                        <div
                            class="grid grid-cols-12 border-b-2 border-secondary bg-primary font-bold text-secondary-content">
                            <div class="col-span-1 px-4 py-2"></div>
                            <div class="col-span-5 px-4 py-2 md:col-span-3">Season</div>
                            <div class="col-span-6 px-4 py-2 md:col-span-8">Episodes</div>
                        </div>
                        <div
                            v-for="season in item.seasons"
                            :key="season.id"
                            class="bg-primary text-sm text-accent-content md:text-base">
                            <div
                                class="grid grid-cols-12"
                                :class="{ 'cursor-pointer': season.episodes.length }"
                                @click="toggleSeason(season)">
                                <div class="col-span-1 px-2 py-2 md:px-4">
                                    <CaretButton
                                        v-if="season.episodes.length"
                                        :is-expanded="expandedSeason?.id !== season.id" />
                                </div>
                                <div class="col-span-5 select-none px-4 py-2 md:col-span-3">
                                    <span v-if="season.seasonNumber == 0">Specials</span>
                                    <span v-else>Season {{ season.seasonNumber }}</span>
                                </div>
                                <div class="col-span-6 select-none px-4 py-2 md:col-span-8">
                                    {{ season.episodes.length }} episodes
                                </div>
                            </div>
                            <div
                                v-if="expandedSeason?.id === season.id"
                                class="w-full bg-tertiary text-tertiary-content">
                                <div class="grid grid-cols-12 border-b-2 border-primary font-bold">
                                    <div class="col-span-1 px-4 py-2 md:col-span-2">
                                        <span class="hidden md:block">Episode</span>
                                        <span class="block md:hidden">#</span>
                                    </div>
                                    <div class="col-span-7 px-4 py-2 md:col-span-5">Title</div>
                                    <div class="col-span-4 px-4 py-2 md:col-span-5">Subtitles</div>
                                </div>
                                <div
                                    v-for="(episode, index) in season.episodes"
                                    :key="episode.id"
                                    class="grid grid-cols-12">
                                    <div class="col-span-1 px-4 py-2 md:col-span-2">
                                        {{ index + 1 }}
                                    </div>
                                    <div class="col-span-7 px-4 py-2 md:col-span-5">
                                        {{ episode.title }}
                                    </div>
                                    <div
                                        v-if="
                                            episode?.fileName &&
                                            getSubtitle(episode.fileName).length
                                        "
                                        class="col-span-4 flex flex-wrap items-center gap-2 md:col-span-5">
                                        <ContextMenu
                                            v-for="(subtitle, jndex) in getSubtitle(
                                                episode.fileName
                                            )"
                                            :key="`${episode.id}-${jndex}`"
                                            :media-type="MEDIA_TYPE.EPISODE"
                                            :media="episode"
                                            :subtitle="subtitle">
                                            <BadgeComponent>
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
        </div>
        <NoMediaNotification v-else />
    </PageLayout>
</template>

<script setup lang="ts">
import { ref, Ref, computed, onMounted, ComputedRef } from 'vue'
import { IFilter, IPagedResult, ISeason, IShow, ISubtitle, MEDIA_TYPE, SETTINGS } from '@/ts'
import useDebounce from '@/composables/useDebounce'
import { useInstanceStore } from '@/store/instance'
import { useSettingStore } from '@/store/setting'
import { useShowStore } from '@/store/show'
import services from '@/services'
import PaginationComponent from '@/components/common/PaginationComponent.vue'
import PageLayout from '@/components/layout/PageLayout.vue'
import SearchComponent from '@/components/common/SearchComponent.vue'
import CaretButton from '@/components/common/CaretButton.vue'
import SortControls from '@/components/common/SortControls.vue'
import BadgeComponent from '@/components/common/BadgeComponent.vue'
import ContextMenu from '@/components/layout/ContextMenu.vue'
import ReloadComponent from '@/components/common/ReloadComponent.vue'
import NoMediaNotification from '@/components/common/NoMediaNotification.vue'

const instanceStore = useInstanceStore()
const showStore = useShowStore()
const settingStore = useSettingStore()

const expandedShow: Ref<boolean | number | null> = ref(null)
const expandedSeason: Ref<ISeason | null> = ref(null)
const subtitles: Ref<ISubtitle[]> = ref([])

const settingsCompleted: ComputedRef<string> = computed(
    () => settingStore.getSetting(SETTINGS.SONARR_SETTINGS_COMPLETED) as string
)
const shows: ComputedRef<IPagedResult<IShow>> = computed(() => showStore.get)
const filter: ComputedRef<IFilter> = computed({
    get: () => showStore.getFilter,
    set: useDebounce((value: IFilter) => {
        showStore.setFilter(value)
    }, 300)
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

const getSubtitle = (fileName: string | null) => {
    return subtitles.value
        .filter((subtitle: ISubtitle) => subtitle.fileName === fileName)
        .slice()
        .sort((a, b) => a.language.localeCompare(b.language))
}

onMounted(async () => {
    await showStore.fetch()
})
</script>
