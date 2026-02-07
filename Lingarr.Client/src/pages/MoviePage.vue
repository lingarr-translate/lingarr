<template>
    <div v-if="settingsCompleted === 'true'" class="w-full">
        <div class="bg-tertiary flex flex-wrap items-center justify-between gap-2 p-4">
            <SearchComponent v-model="filter" />
            <div
                class="flex w-full flex-col gap-2 md:w-fit md:flex-row md:items-center md:justify-between md:space-x-2">
                <ToggleButton
                    v-model="navigateToDetails"
                    size="small"
                    label="Open details on request" />
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
            <div class="border-accent grid grid-cols-12 border-b font-bold">
                <div class="col-span-5 px-4 py-2">{{ translate('movies.title') }}</div>
                <div class="col-span-4 px-4 py-2">{{ translate('movies.subtitles') }}</div>
                <div class="col-span-1 px-4 py-2">
                    {{ translate('movies.exclude') }}
                </div>
                <div class="col-span-1 px-4 py-2">
                    {{ translate('movies.ageThreshold') }}
                </div>
                <div class="col-span-1 flex justify-end px-4 py-2">
                    <ReloadComponent @toggle:update="movieStore.fetch()" />
                </div>
            </div>
            <div v-for="item in movies.items" :key="item.id">
                <div class="border-accent grid grid-cols-12 border-b">
                    <div class="col-span-5 px-4 py-2">
                        {{ item.title }}
                    </div>
                    <div class="col-span-4 flex flex-wrap items-center gap-2 px-4 py-2">
                        <ContextMenu
                            v-for="(subtitle, index) in item.subtitles"
                            :key="`${index}-${subtitle.fileName}`"
                            :subtitle="subtitle"
                            :media="item"
                            :media-type="MEDIA_TYPE.MOVIE"
                            @update:toggle="toggleMovie(item)">
                            <BadgeComponent>
                                {{ subtitle.language.toUpperCase() }}
                                <span v-if="subtitle.caption" class="text-primary-content/50">
                                    - {{ subtitle.caption.toUpperCase() }}
                                </span>
                            </BadgeComponent>
                        </ContextMenu>
                    </div>
                    <div class="col-span-1 flex flex-wrap items-center gap-2 px-4 py-2">
                        <ToggleButton
                            v-model="item.excludeFromTranslation"
                            size="small"
                            @toggle:update="() => movieStore.exclude(MEDIA_TYPE.MOVIE, item.id)" />
                    </div>
                    <div class="col-span-2 flex items-center px-4 py-2" @click.stop>
                        <InputComponent
                            :model-value="item?.translationAgeThreshold"
                            :placeholder="translate('movies.hours')"
                            class="w-14"
                            size="sm"
                            type="number"
                            validation-type="number"
                            @update:value="
                                (value) => {
                                    item.translationAgeThreshold = value
                                    movieStore.updateThreshold(MEDIA_TYPE.MOVIE, item.id, value)
                                }
                            " />
                    </div>
                </div>
            </div>
        </div>

        <PaginationComponent
            v-if="movies.totalCount"
            v-model="filter"
            :total-count="movies.totalCount"
            :page-size="movies.pageSize" />
    </div>
    <NoMediaNotification v-else />
</template>

<script setup lang="ts">
import { computed, onMounted, ComputedRef } from 'vue'
import { IFilter, IMovie, IPagedResult, MEDIA_TYPE, SETTINGS } from '@/ts'
import useDebounce from '@/composables/useDebounce'
import { useMovieStore } from '@/store/movie'
import { useSettingStore } from '@/store/setting'
import { useInstanceStore } from '@/store/instance'
import PaginationComponent from '@/components/common/PaginationComponent.vue'
import BadgeComponent from '@/components/common/BadgeComponent.vue'
import SortControls from '@/components/common/SortControls.vue'
import SearchComponent from '@/components/common/SearchComponent.vue'
import ContextMenu from '@/components/layout/ContextMenu.vue'
import ReloadComponent from '@/components/common/ReloadComponent.vue'
import NoMediaNotification from '@/components/common/NoMediaNotification.vue'
import ToggleButton from '@/components/common/ToggleButton.vue'
import InputComponent from '@/components/common/InputComponent.vue'

const movieStore = useMovieStore()
const settingStore = useSettingStore()
const instanceStore = useInstanceStore()

const settingsCompleted: ComputedRef<string> = computed(
    () => settingStore.getSetting(SETTINGS.RADARR_SETTINGS_COMPLETED) as string
)

const navigateToDetails = computed({
    get: () => settingStore.getSetting(SETTINGS.NAVIGATE_TO_DETAILS_ON_REQUEST) as string,
    set: (value: string) => {
        settingStore.updateSetting(SETTINGS.NAVIGATE_TO_DETAILS_ON_REQUEST, value, true)
    }
})

const movies: ComputedRef<IPagedResult<IMovie>> = computed(() => movieStore.get)
const filter: ComputedRef<IFilter> = computed({
    get: () => movieStore.getFilter,
    set: useDebounce((value: IFilter) => {
        movieStore.setFilter(value)
    }, 300)
})

const toggleMovie = useDebounce(async (movie: IMovie) => {
    instanceStore.setPoster({ content: movie, type: 'movie' })
}, 1000)

onMounted(async () => {
    await movieStore.fetch()
})
</script>
