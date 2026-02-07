<template>
    <div v-if="settingsCompleted === 'true'" class="w-full">
        <div class="bg-tertiary flex flex-wrap items-center justify-between gap-2 p-4">
            <SearchComponent v-model="filter" />
            <div
                class="flex w-full flex-col gap-2 md:w-fit md:flex-row md:items-center md:justify-between md:space-x-2">
                <ContextMenu
                    v-if="isSelectMode && showStore.selectedShows.length > 0"
                    @select="handleTranslate">
                    <ButtonComponent size="sm" variant="accent">
                        Translate ({{ showStore.selectedShows.length }})
                    </ButtonComponent>
                </ContextMenu>
                <ButtonComponent size="sm" variant="accent" @click="toggleSelectMode">
                    {{ isSelectMode ? 'Cancel' : 'Select' }}
                </ButtonComponent>
                <ToggleButton
                    v-model="navigateToDetails"
                    size="small"
                    label="Open details on request" />
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
            <!-- Shows -->
            <div class="border-accent grid grid-cols-12 border-b font-bold">
                <div :class="isSelectMode ? 'col-span-7' : 'col-span-8'" class="px-4 py-2">
                    {{ translate('tvShows.title') }}
                </div>
                <div class="col-span-1 px-4 py-2">
                    <span class="hidden md:block">
                        {{ translate('tvShows.exclude') }}
                    </span>
                    <span class="block md:hidden">⊘</span>
                </div>
                <div class="col-span-1 px-4 py-2">
                    {{ translate('tvShows.ageThreshold') }}
                </div>
                <div class="col-span-2 flex justify-end px-4 py-2">
                    <ReloadComponent @toggle:update="showStore.fetch()" />
                </div>
                <div
                    v-if="isSelectMode"
                    class="col-span-1 flex items-center justify-center px-4 py-2">
                    <CheckboxComponent
                        :model-value="showStore.selectAll"
                        @change="showStore.toggleSelectAll()" />
                </div>
            </div>
            <div v-for="item in shows.items" :key="item.id">
                <div
                    class="border-accent grid cursor-pointer grid-cols-12 border-b"
                    @click="toggleShow(item)">
                    <div
                        :class="isSelectMode ? 'col-span-7' : 'col-span-8'"
                        class="flex items-center px-4 py-2">
                        <CaretButton :is-expanded="expandedShow !== item.id" class="pr-2" />
                        {{ item.title }}
                    </div>
                    <div class="col-span-1 flex items-center px-4 py-2" @click.stop>
                        <ToggleButton
                            v-model="item.excludeFromTranslation"
                            size="small"
                            @toggle:update="() => showStore.exclude(MEDIA_TYPE.SHOW, item.id)" />
                    </div>
                    <div class="col-span-3 flex items-center px-4 py-2" @click.stop>
                        <InputComponent
                            :model-value="item.translationAgeThreshold ?? null"
                            :placeholder="translate('tvShows.hours')"
                            class="w-14"
                            size="sm"
                            type="number"
                            validation-type="number"
                            @update:value="
                                (value) => {
                                    item.translationAgeThreshold = value
                                    showStore.updateThreshold(MEDIA_TYPE.SHOW, item.id, value)
                                }
                            " />
                    </div>
                    <div
                        v-if="isSelectMode"
                        class="col-span-1 flex items-center justify-center px-4 py-2"
                        @click.stop>
                        <CheckboxComponent
                            :model-value="
                                showStore.selectedShows.some((s) => s.id === item.id)
                            "
                            @change="showStore.toggleSelect(item)" />
                    </div>
                </div>
                <SeasonTable v-if="expandedShow === item.id" :seasons="item.seasons" />
            </div>
        </div>

        <PaginationComponent
            v-if="shows.totalCount"
            v-model="filter"
            :total-count="shows.totalCount"
            :page-size="shows.pageSize" />
    </div>
    <NoMediaNotification v-else />
</template>

<script setup lang="ts">
import { ref, Ref, computed, onMounted, ComputedRef } from 'vue'
import { IFilter, ILanguage, IPagedResult, IShow, ISubtitle, MEDIA_TYPE, SETTINGS } from '@/ts'
import useDebounce from '@/composables/useDebounce'
import { useInstanceStore } from '@/store/instance'
import { useSettingStore } from '@/store/setting'
import { useShowStore } from '@/store/show'
import { useTranslateStore } from '@/store/translate'
import services from '@/services'
import PaginationComponent from '@/components/common/PaginationComponent.vue'
import SearchComponent from '@/components/common/SearchComponent.vue'
import CaretButton from '@/components/common/CaretButton.vue'
import SortControls from '@/components/common/SortControls.vue'
import ReloadComponent from '@/components/common/ReloadComponent.vue'
import NoMediaNotification from '@/components/common/NoMediaNotification.vue'
import ToggleButton from '@/components/common/ToggleButton.vue'
import SeasonTable from '@/components/features/show/SeasonTable.vue'
import InputComponent from '@/components/common/InputComponent.vue'
import CheckboxComponent from '@/components/common/CheckboxComponent.vue'
import ContextMenu from '@/components/layout/ContextMenu.vue'
import ButtonComponent from '@/components/common/ButtonComponent.vue'

const instanceStore = useInstanceStore()
const showStore = useShowStore()
const settingStore = useSettingStore()
const translateStore = useTranslateStore()
const expandedShow: Ref<boolean | number | null> = ref(null)

const settingsCompleted: ComputedRef<string> = computed(
    () => settingStore.getSetting(SETTINGS.SONARR_SETTINGS_COMPLETED) as string
)

const navigateToDetails = computed({
    get: () => settingStore.getSetting(SETTINGS.NAVIGATE_TO_DETAILS_ON_REQUEST) as string,
    set: (value: string) => {
        settingStore.updateSetting(SETTINGS.NAVIGATE_TO_DETAILS_ON_REQUEST, value, true)
    }
})

const shows: ComputedRef<IPagedResult<IShow>> = computed(() => showStore.get)
const filter: ComputedRef<IFilter> = computed({
    get: () => showStore.getFilter,
    set: useDebounce((value: IFilter) => {
        showStore.setFilter(value)
    }, 300)
})

const toggleShow = async (show: IShow) => {
    if (expandedShow.value === show.id) {
        expandedShow.value = null
        return
    }
    instanceStore.setPoster({ content: show, type: 'show' })
    expandedShow.value = show.id
}

const isSelectMode = ref(false)

const toggleSelectMode = () => {
    isSelectMode.value = !isSelectMode.value
    if (!isSelectMode.value) {
        showStore.clearSelection()
    }
}

const getSubtitle = (subtitles: ISubtitle[], fileName: string | null) => {
    if (!fileName) return null
    return subtitles.find(
        (subtitle: ISubtitle) =>
            subtitle.fileName.toLocaleLowerCase().includes(fileName.toLocaleLowerCase()) &&
            subtitle.language &&
            subtitle.language.trim() !== ''
    ) ?? null
}

const handleTranslate = async (language: ILanguage) => {
    for (const show of showStore.selectedShows) {
        for (const season of show.seasons) {
            if (!season.path) {
                continue
            }
            const subtitles = await services.subtitle.collect<ISubtitle[]>(season.path)
            for (const episode of season.episodes) {
                const subtitle = getSubtitle(subtitles, episode.fileName ?? null)
                if (subtitle) {
                    await translateStore.translateSubtitle(
                        episode.id,
                        subtitle,
                        subtitle.language,
                        language,
                        MEDIA_TYPE.EPISODE
                    )
                }
            }
        }
    }
    showStore.clearSelection()
    isSelectMode.value = false
}

onMounted(async () => {
    await showStore.fetch()
})
</script>
