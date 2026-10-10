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
                    <ReloadComponent @toggle:update="showStore.fetch()" />
                </div>
            </div>
            <div class="media-toolbar-actions">
                <ToggleButton
                    v-model="navigateToDetails"
                    size="small"
                    label="Open details on request" />
                <div class="media-toolbar-selection">
                    <ContextMenu
                        v-if="isSelectMode && showStore.selectedShows.length > 0"
                        @select="handleTranslate">
                        <ButtonComponent size="sm" variant="accent">
                            Translate ({{ showStore.selectedShows.length }})
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
                        @click="showStore.includeAll(MEDIA_TYPE.SHOW, true)">
                        Include all
                    </ButtonComponent>
                    <ButtonComponent
                        size="sm"
                        variant="secondary"
                        v-tooltip="
                            'Applies to the entire library, including items outside this page.'
                        "
                        @click="showStore.includeAll(MEDIA_TYPE.SHOW, false)">
                        Exclude all
                    </ButtonComponent>
                    <span class="text-secondary-content px-2 text-sm">
                        {{ showStore.includeSummary.included }}/{{ showStore.includeSummary.total }}
                        included
                    </span>
                </div>
            </div>
        </div>

        <div
            class="media-list media-list--shows mx-4 sm:mx-6"
            :class="{ 'is-selecting': isSelectMode }">
            <!-- Shows -->
            <div class="media-table-heading media-library-heading border-b font-semibold">
                <div>Title</div>
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
                <div v-if="isSelectMode" class="media-select-cell">
                    <CheckboxComponent
                        aria-label="Select all items on this page"
                        :model-value="showStore.selectAll"
                        @change="showStore.toggleSelectAll()" />
                </div>
            </div>
            <div v-for="item in shows.items" :key="item.id">
                <div
                    class="media-row media-library-row cursor-pointer border-b"
                    @click="toggleShow(item)">
                    <div class="media-title flex items-center gap-2">
                        <CaretButton :is-expanded="expandedShow !== item.id" class="pr-2" />
                        <button
                            type="button"
                            class="min-h-11 min-w-0 flex-1 cursor-pointer text-left font-medium"
                            :aria-expanded="expandedShow === item.id"
                            @click.stop="toggleShow(item)">
                            {{ item.title }}
                        </button>
                        <ActiveTranslationBadge :show="item" />
                        <span
                            v-if="isSelectMode"
                            class="media-select-inline ml-auto shrink-0"
                            @click.stop>
                            <CheckboxComponent
                                :aria-label="`Select ${item.title}`"
                                :model-value="showStore.selectedShows.some((s) => s.id === item.id)"
                                @change="showStore.toggleSelect(item)" />
                        </span>
                    </div>
                    <div class="media-controls">
                        <div class="media-control media-include" @click.stop>
                            <ToggleButton
                                v-model="item.includeInTranslation"
                                :aria-label="`Include ${item.title} in automatic translation`"
                                size="small"
                                @toggle:update="
                                    () =>
                                        showStore.include(
                                            MEDIA_TYPE.SHOW,
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
                                :model-value="item.translationAgeThreshold ?? null"
                                placeholder="hours"
                                :aria-label="`Translation delay in hours for ${item.title}`"
                                class="w-20"
                                size="sm"
                                :validation-type="INPUT_VALIDATION_TYPE.NUMBER"
                                :type="INPUT_TYPE.NUMBER"
                                @update:value="
                                    (value) => {
                                        item.translationAgeThreshold = value
                                        showStore.updateThreshold(MEDIA_TYPE.SHOW, item.id, value)
                                    }
                                " />
                            <span
                                class="media-control-label text-secondary-content text-xs font-semibold">
                                Delay (h)
                            </span>
                        </div>
                    </div>
                    <div v-if="isSelectMode" class="media-select-cell" @click.stop>
                        <CheckboxComponent
                            :aria-label="`Select ${item.title}`"
                            :model-value="showStore.selectedShows.some((s) => s.id === item.id)"
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
import {
    IFilter,
    ILanguage,
    INPUT_TYPE,
    INPUT_VALIDATION_TYPE,
    IPagedResult,
    IShow,
    MEDIA_TYPE,
    SETTINGS
} from '@/ts'
import useDebounce from '@/composables/useDebounce'
import { useInstanceStore } from '@/store/instance'
import { useSettingStore } from '@/store/setting'
import { useShowStore } from '@/store/show'
import { useTranslateStore } from '@/store/translate'
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
import ActiveTranslationBadge from '@/components/common/ActiveTranslationBadge.vue'

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

const handleTranslate = async (language: ILanguage) => {
    await translateStore.bulkTranslate(
        showStore.selectedShows.map((s) => s.id),
        language.code,
        MEDIA_TYPE.SHOW
    )
    showStore.clearSelection()
    isSelectMode.value = false
}

onMounted(async () => {
    await showStore.fetch()
})
</script>
