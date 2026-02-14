<template>
    <div v-if="settingsCompleted === 'true'" class="w-full">
        <div class="flex flex-wrap items-center justify-between gap-2 bg-tertiary p-4">
            <SearchComponent v-model="filter" />
            <div
                class="flex w-full flex-col gap-2 md:w-fit md:flex-row md:items-center md:justify-between md:space-x-2">
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
            </div>
        </div>

        <div class="w-full px-4">
            <div class="grid grid-cols-12 border-b border-accent font-bold">
                <div :class="isSelectMode ? 'col-span-4' : 'col-span-5'" class="px-4 py-2">
                    Title
                </div>
                <div class="col-span-4 px-4 py-2">Subtitles</div>
                <div class="col-span-1 px-4 py-2">Exclude</div>
                <div class="col-span-1 px-4 py-2">Delay</div>
                <div class="col-span-1 flex justify-end px-4 py-2">
                    <ReloadComponent @toggle:update="movieStore.fetch()" />
                </div>
                <div
                    v-if="isSelectMode"
                    class="col-span-1 flex items-center justify-center px-4 py-2">
                    <CheckboxComponent
                        :model-value="movieStore.selectAll"
                        @change="movieStore.toggleSelectAll()" />
                </div>
            </div>
            <div v-for="item in movies.items" :key="item.id">
                <div class="grid grid-cols-12 border-b border-accent">
                    <div :class="isSelectMode ? 'col-span-4' : 'col-span-5'" class="px-4 py-2">
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
                            placeholder="hours"
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
                    <div
                        v-if="isSelectMode"
                        class="col-span-1 flex items-center justify-center px-4 py-2">
                        <CheckboxComponent
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
import { IFilter, ILanguage, IMovie, IPagedResult, MEDIA_TYPE, SETTINGS } from '@/ts'
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
