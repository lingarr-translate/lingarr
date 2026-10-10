<template>
    <div v-if="settingsCompleted === 'true'" class="w-full">
        <div class="ui-toolbar">
            <SearchComponent v-model="filter" />
            <div class="flex w-full flex-wrap items-center gap-2">
                <ToggleButton
                    v-model="navigateToDetails"
                    size="small"
                    label="Open details on request" />
                <ContextMenu
                    v-if="isSelectMode && movieStore.selectedMovies.length > 0"
                    @select="handleTranslate">
                    <ButtonComponent size="sm" variant="accent">
                        Translate ({{ movieStore.selectedMovies.length }})
                    </ButtonComponent>
                </ContextMenu>
                <ButtonComponent size="sm" variant="accent" @click="toggleSelectMode">
                    {{ isSelectMode ? 'Cancel' : 'Translate multiple' }}
                </ButtonComponent>
                <ButtonComponent
                    size="sm"
                    variant="secondary"
                    v-tooltip="'Applies to the entire library, including items outside this page.'"
                    @click="movieStore.includeAll(MEDIA_TYPE.MOVIE, true)">
                    Include all
                </ButtonComponent>
                <ButtonComponent
                    size="sm"
                    variant="secondary"
                    v-tooltip="'Applies to the entire library, including items outside this page.'"
                    @click="movieStore.includeAll(MEDIA_TYPE.MOVIE, false)">
                    Exclude all
                </ButtonComponent>
                <span class="text-secondary-content px-2 text-sm">
                    {{ movieStore.includeSummary.included }}/{{ movieStore.includeSummary.total }}
                    included
                </span>
                <div class="flex w-full flex-wrap items-center gap-2 sm:ml-auto sm:w-auto">
                    <SortControls
                        v-model="filter"
                        :options="[
                            {
                                label: 'Sort by Title',
                                value: 'Title'
                            },
                            {
                                label: 'Sort by Added',
                                value: 'DateAdded'
                            }
                        ]" />
                    <ReloadComponent @toggle:update="movieStore.fetch()" />
                </div>
            </div>
        </div>

        <div class="w-full px-4 sm:px-6">
            <div class="media-table-heading hidden border-b font-semibold xl:grid xl:grid-cols-12">
                <div :class="isSelectMode ? 'col-span-4' : 'col-span-5'" class="px-4 py-2">
                    Title
                </div>
                <div class="col-span-4 px-4 py-2">Subtitles</div>
                <div class="col-span-1 px-4 py-2">Include</div>
                <div class="col-span-2 px-4 py-2">
                    Delay
                    <button
                        type="button"
                        class="ui-border ml-1 inline-flex h-8 w-8 items-center justify-center rounded-full border text-xs normal-case"
                        aria-label="About translation delay"
                        v-tooltip="
                            'Minimum media age in hours before automatic translation. Leave empty to use the global delay.'
                        ">
                        ?
                    </button>
                </div>
                <div
                    v-if="isSelectMode"
                    class="col-span-1 flex items-center justify-center px-4 py-2">
                    <CheckboxComponent
                        aria-label="Select all items on this page"
                        :model-value="movieStore.selectAll"
                        @change="movieStore.toggleSelectAll()" />
                </div>
            </div>
            <div v-for="item in movies.items" :key="item.id">
                <div
                    class="media-row hover:bg-accent/5 flex flex-wrap items-center gap-x-3 gap-y-2 border-b py-3 transition-colors xl:grid xl:grid-cols-12 xl:gap-0 xl:py-2">
                    <div
                        :class="isSelectMode ? 'xl:col-span-4' : 'xl:col-span-5'"
                        class="flex w-full items-center gap-2 xl:w-auto xl:px-4 xl:py-2">
                        <span class="font-medium">{{ item.title }}</span>
                        <ActiveTranslationBadge
                            :media-id="item.id"
                            :media-type="MEDIA_TYPE.MOVIE" />
                        <span v-if="isSelectMode" class="ml-auto xl:hidden">
                            <CheckboxComponent
                                :aria-label="`Select ${item.title}`"
                                :model-value="
                                    movieStore.selectedMovies.some((m) => m.id === item.id)
                                "
                                @change="movieStore.toggleSelect(item)" />
                        </span>
                    </div>
                    <div
                        :class="item.subtitles?.length ? 'flex' : 'hidden xl:flex'"
                        class="w-full flex-wrap items-center gap-2 xl:col-span-4 xl:w-auto xl:px-4 xl:py-2">
                        <ContextMenu
                            v-for="(subtitle, index) in item.subtitles"
                            :key="`${index}-${subtitle.fileName}`"
                            :subtitle="subtitle"
                            :media="item"
                            :media-type="MEDIA_TYPE.MOVIE"
                            @update:toggle="toggleMovie(item)">
                            <BadgeComponent>
                                {{ subtitle.language.toUpperCase() }}
                                <span v-if="subtitle.caption" class="text-secondary-content">
                                    - {{ subtitle.caption.toUpperCase() }}
                                </span>
                            </BadgeComponent>
                        </ContextMenu>
                    </div>
                    <div class="flex items-center gap-2 xl:col-span-1 xl:px-4 xl:py-2">
                        <ToggleButton
                            v-model="item.includeInTranslation"
                            :aria-label="`Include ${item.title} in automatic translation`"
                            size="small"
                            @toggle:update="
                                () =>
                                    movieStore.include(
                                        MEDIA_TYPE.MOVIE,
                                        item.id,
                                        item.includeInTranslation
                                    )
                            " />
                        <span
                            class="text-secondary-content text-xs font-semibold tracking-wide uppercase xl:hidden">
                            Include
                        </span>
                    </div>
                    <div class="flex items-center gap-2 xl:col-span-2 xl:px-4 xl:py-2" @click.stop>
                        <InputComponent
                            :model-value="item?.translationAgeThreshold"
                            placeholder="hours"
                            :aria-label="`Translation delay in hours for ${item.title}`"
                            class="w-20"
                            size="sm"
                            :validation-type="INPUT_VALIDATION_TYPE.NUMBER"
                            :type="INPUT_TYPE.NUMBER"
                            @update:value="
                                (value) => {
                                    item.translationAgeThreshold = value
                                    movieStore.updateThreshold(MEDIA_TYPE.MOVIE, item.id, value)
                                }
                            " />
                        <span
                            class="text-secondary-content text-xs font-semibold tracking-wide uppercase xl:hidden">
                            Delay (hours)
                        </span>
                    </div>
                    <div
                        v-if="isSelectMode"
                        class="hidden items-center justify-center px-4 py-2 xl:col-span-1 xl:flex">
                        <CheckboxComponent
                            :aria-label="`Select ${item.title}`"
                            :model-value="movieStore.selectedMovies.some((m) => m.id === item.id)"
                            @change="movieStore.toggleSelect(item)" />
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
import { ref, computed, onMounted, ComputedRef } from 'vue'
import {
    IFilter,
    ILanguage,
    IMovie,
    INPUT_TYPE,
    INPUT_VALIDATION_TYPE,
    IPagedResult,
    MEDIA_TYPE,
    SETTINGS
} from '@/ts'
import useDebounce from '@/composables/useDebounce'
import { useMovieStore } from '@/store/movie'
import { useSettingStore } from '@/store/setting'
import { useInstanceStore } from '@/store/instance'
import { useTranslateStore } from '@/store/translate'
import PaginationComponent from '@/components/common/PaginationComponent.vue'
import BadgeComponent from '@/components/common/BadgeComponent.vue'
import SortControls from '@/components/common/SortControls.vue'
import SearchComponent from '@/components/common/SearchComponent.vue'
import ContextMenu from '@/components/layout/ContextMenu.vue'
import ReloadComponent from '@/components/common/ReloadComponent.vue'
import NoMediaNotification from '@/components/common/NoMediaNotification.vue'
import ToggleButton from '@/components/common/ToggleButton.vue'
import InputComponent from '@/components/common/InputComponent.vue'
import CheckboxComponent from '@/components/common/CheckboxComponent.vue'
import ButtonComponent from '@/components/common/ButtonComponent.vue'
import ActiveTranslationBadge from '@/components/common/ActiveTranslationBadge.vue'

const movieStore = useMovieStore()
const settingStore = useSettingStore()
const instanceStore = useInstanceStore()
const translateStore = useTranslateStore()

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

const isSelectMode = ref(false)

const toggleSelectMode = () => {
    isSelectMode.value = !isSelectMode.value
    if (!isSelectMode.value) {
        movieStore.clearSelection()
    }
}

const handleTranslate = async (language: ILanguage) => {
    await translateStore.bulkTranslate(
        movieStore.selectedMovies.map((movie) => movie.id),
        language.code,
        MEDIA_TYPE.MOVIE
    )
    movieStore.clearSelection()
    isSelectMode.value = false
}

onMounted(async () => {
    await movieStore.fetch()
})
</script>
