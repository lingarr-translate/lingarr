<template>
    <PageLayout>
        <div v-if="settingsCompleted === 'true'" class="w-full">
            <div class="bg-tertiary flex flex-wrap items-center justify-between gap-2 p-4">
                <SearchComponent v-model="filter" />
                <div class="flex w-full justify-between space-x-2 md:w-fit">
                    <SortControls
                        v-model="filter"
                        :options="[
                            {
                                label: translate('common.sortByTitle'),
                                value: 'Title'
                            },
                            {
                                label: translate('common.sortByAdded'),
                                value: 'DateAdded'
                            }
                        ]" />
                </div>
            </div>

            <div class="w-full px-4">
                <!-- Shows -->
                <div class="border-accent grid grid-cols-12 border-b font-bold">
                    <div class="col-span-1 px-4 py-2"></div>
                    <div class="col-span-7 px-4 py-2">{{ translate('tvShows.title') }}</div>
                    <div class="col-span-2 px-4 py-2">
                        {{ translate('tvShows.exclude') }}
                    </div>
                    <div class="col-span-2 flex justify-end px-4 py-2">
                        <ReloadComponent @toggle:update="showStore.fetch()" />
                    </div>
                </div>
                <div v-for="item in shows.items" :key="item.id">
                    <div
                        class="border-accent grid cursor-pointer grid-cols-12 border-b"
                        @click="toggleShow(item)">
                        <div class="col-span-1 px-2 py-2 md:px-4">
                            <CaretButton :is-expanded="expandedShow !== item.id" />
                        </div>
                        <div class="col-span-7 px-4 py-2">
                            {{ item.title }}
                        </div>
                        <div class="col-span-4 flex items-center px-4 py-2" @click.stop>
                            <ToggleButton
                                v-model="item.excludeFromTranslation"
                                size="small"
                                @toggle:update="
                                    () => showStore.exclude(MEDIA_TYPE.SHOW, item.id)
                                " />
                        </div>
                    </div>
                    <div v-if="expandedShow === item.id" class="bg-secondary p-4">
                        <div
                            class="border-secondary bg-primary text-secondary-content grid grid-cols-12 border-b-2 font-bold">
                            <div class="col-span-1 px-4 py-2"></div>
                            <div class="col-span-5 px-4 py-2 md:col-span-3">
                                {{ translate('tvShows.season') }}
                            </div>
                            <div class="col-span-6 flex justify-between px-4 py-2 md:col-span-8">
                                <span>{{ translate('tvShows.episodes') }}</span>
                                <span>{{ translate('tvShows.exclude') }}</span>
                            </div>
                        </div>
                        <!-- Seasons -->
                        <div
                            v-for="season in item.seasons"
                            :key="season.id"
                            class="bg-primary text-accent-content text-sm md:text-base">
                            <div
                                class="grid grid-cols-12"
                                :class="{ 'cursor-pointer': season.episodes.length }"
                                @click="toggleSeason(season)">
                                <div class="col-span-1 px-2 py-2 md:px-4">
                                    <CaretButton
                                        v-if="season.episodes.length"
                                        :is-expanded="expandedSeason?.id !== season.id" />
                                </div>
                                <div class="col-span-5 px-4 py-2 select-none md:col-span-3">
                                    <span v-if="season.seasonNumber == 0">
                                        {{ translate('tvShows.specials') }}
                                    </span>
                                    <span v-else>
                                        {{ translate('tvShows.season') }} {{ season.seasonNumber }}
                                    </span>
                                </div>
                                <div
                                    class="col-span-6 flex justify-between px-4 py-2 select-none md:col-span-8">
                                    <span>
                                        {{ season.episodes.length }}
                                        {{ translate('tvShows.episodesLine') }}
                                    </span>
                                    <span @click.stop>
                                        <ToggleButton
                                            v-model="season.excludeFromTranslation"
                                            size="small"
                                            @toggle:update="
                                                () =>
                                                    showStore.exclude(MEDIA_TYPE.SEASON, season.id)
                                            " />
                                    </span>
                                </div>
                            </div>
                            <!-- Episodes -->
                            <div
                                v-if="expandedSeason?.id === season.id"
                                class="bg-tertiary text-tertiary-content w-full">
                                <div class="border-primary grid grid-cols-12 border-b-2 font-bold">
                                    <div class="col-span-1 px-4 py-2 md:col-span-2">
                                        <span class="hidden md:block">
                                            {{ translate('tvShows.episode') }}
                                        </span>
                                        <span class="block md:hidden">#</span>
                                    </div>
                                    <div class="col-span-7 px-4 py-2 md:col-span-5">
                                        {{ translate('tvShows.episodeTitle') }}
                                    </div>
                                    <div
                                        class="col-span-4 flex justify-between px-4 py-2 md:col-span-5">
                                        <span>{{ translate('tvShows.episodeSubtitles') }}</span>
                                        <span>{{ translate('tvShows.exclude') }}</span>
                                    </div>
                                </div>
                                <div
                                    v-for="episode in season.episodes"
                                    :key="episode.id"
                                    class="grid grid-cols-12">
                                    <div class="col-span-1 px-4 py-2 md:col-span-2">
                                        {{ episode.episodeNumber }}
                                    </div>
                                    <div class="col-span-7 px-4 py-2 md:col-span-5">
                                        {{ episode.title }}
                                    </div>
                                    <div class="col-span-4 flex justify-between pr-4 md:col-span-5">
                                        <div
                                            v-if="episode?.fileName"
                                            class="flex flex-wrap items-center gap-2">
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
                                                    <span
                                                        v-if="subtitle.caption"
                                                        class="text-primary-content/50">
                                                        - {{ subtitle.caption.toUpperCase() }}
                                                    </span>
                                                </BadgeComponent>
                                            </ContextMenu>
                                        </div>
                                        <div class="col-span-1 px-1 py-2 md:col-span-1">
                                            <ToggleButton
                                                v-model="episode.excludeFromTranslation"
                                                size="small"
                                                @toggle:update="
                                                    () =>
                                                        showStore.exclude(
                                                            MEDIA_TYPE.EPISODE,
                                                            episode.id
                                                        )
                                                " />
                                        </div>
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
import ToggleButton from '@/components/common/ToggleButton.vue'

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
    if (!fileName) return null
    return subtitles.value
        .filter((subtitle: ISubtitle) => subtitle.fileName.includes(fileName))
        .slice()
        .sort((a, b) => a.language.localeCompare(b.language))
}

onMounted(async () => {
    await showStore.fetch()
})
</script>
