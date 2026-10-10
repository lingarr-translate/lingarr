<template>
    <div v-if="settingsCompleted === 'true'" class="media-library w-full">
        <div class="media-toolbar">
            <div class="media-toolbar-query">
                <SearchComponent v-model="filter" />
                <div class="media-toolbar-sort">
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
            <div class="media-toolbar-actions">
                <ToggleButton
                    v-model="navigateToDetails"
                    size="small"
                    label="Open details on request" />
                <div class="media-toolbar-selection">
                    <CheckboxComponent
                        v-if="isSelectMode"
                        label="Select all on this page"
                        :model-value="movieStore.selectAll"
                        @change="movieStore.toggleSelectAll()" />
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
                </div>
                <div class="media-toolbar-inclusion">
                    <ButtonComponent
                        size="sm"
                        variant="secondary"
                        v-tooltip="
                            'Applies to the entire library, including items outside this page.'
                        "
                        @click="movieStore.includeAll(MEDIA_TYPE.MOVIE, true)">
                        Include all
                    </ButtonComponent>
                    <ButtonComponent
                        size="sm"
                        variant="secondary"
                        v-tooltip="
                            'Applies to the entire library, including items outside this page.'
                        "
                        @click="movieStore.includeAll(MEDIA_TYPE.MOVIE, false)">
                        Exclude all
                    </ButtonComponent>
                    <span class="text-secondary-content px-2 text-sm">
                        {{ movieStore.includeSummary.included }}/{{
                            movieStore.includeSummary.total
                        }}
                        included
                    </span>
                </div>
            </div>
        </div>

        <div
            class="media-list media-list--movies mx-4 sm:mx-6"
            :class="{ 'is-selecting': isSelectMode }">
            <div class="media-table-heading media-library-heading border-b font-semibold">
                <div>Title</div>
                <div>Subtitles</div>
                <div>Include</div>
                <div class="media-delay-heading">
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
            </div>
            <div v-for="item in movies.items" :key="item.id">
                <div class="media-row media-library-row border-b transition-colors">
                    <div class="media-title flex items-center gap-2">
                        <span class="font-medium">{{ item.title }}</span>
                        <ActiveTranslationBadge
                            :media-id="item.id"
                            :media-type="MEDIA_TYPE.MOVIE" />
                        <span v-if="isSelectMode" class="media-select-inline ml-auto shrink-0">
                            <CheckboxComponent
                                :aria-label="`Select ${item.title}`"
                                :model-value="
                                    movieStore.selectedMovies.some((m) => m.id === item.id)
                                "
                                @change="movieStore.toggleSelect(item)" />
                        </span>
                    </div>
                    <div class="media-subtitles flex-wrap items-center gap-2">
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
                    <div class="media-controls">
                        <div class="media-control media-include">
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
                                class="media-control-label text-secondary-content text-xs font-semibold">
                                Include
                            </span>
                        </div>
                        <div class="media-control media-delay" @click.stop>
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
                                class="media-control-label text-secondary-content text-xs font-semibold">
                                Delay (h)
                            </span>
                        </div>
                    </div>
                    <div v-if="isSelectMode" class="media-select-cell">
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
