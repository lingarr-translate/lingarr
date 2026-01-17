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
                <div class="flex items-center gap-2">
                    <span class="text-sm">{{ translate('tvShows.includeAll') }}:</span>
                    <ToggleButton
                        :model-value="showStore.includeSummary.allIncluded"
                        size="small"
                        @toggle:update="handleIncludeAll" />
                    <span class="text-xs text-primary-content/70">
                        ({{ showStore.includeSummary.included }}/{{ showStore.includeSummary.total }})
                    </span>
                </div>
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
            <div class="grid grid-cols-12 border-b border-accent font-bold">
                <div :class="isSelectMode ? 'col-span-7' : 'col-span-8'" class="px-4 py-2">
                    Title
                </div>
                <div class="col-span-1 px-4 py-2">
                    <span class="hidden md:block">
                        {{ translate('tvShows.include') }}
                    </span>
                    <span class="block md:hidden">✓</span>
                </div>
                <div class="col-span-1 px-4 py-2">Delay</div>
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
                    class="grid cursor-pointer grid-cols-12 border-b border-accent"
                    @click="toggleShow(item)">
                    <div
                        :class="isSelectMode ? 'col-span-7' : 'col-span-8'"
                        class="flex items-center px-4 py-2">
                        <CaretButton :is-expanded="expandedShow !== item.id" class="pr-2" />
                        {{ item.title }}
                    </div>
                    <div class="col-span-1 flex items-center px-4 py-2" @click.stop>
                        <ToggleButton
                            :model-value="item.excludeFromTranslation === 'false'"
                            size="small"
                            @toggle:update="() => handleIncludeToggle(item)" />
                    </div>
                    <div class="col-span-3 flex items-center px-4 py-2" @click.stop>
                        <InputComponent
                            :model-value="item.translationAgeThreshold ?? null"
                            placeholder="hours"
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
import { IFilter, ILanguage, IPagedResult, IShow, MEDIA_TYPE, SETTINGS } from '@/ts'
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
}

const handleIncludeToggle = async (show: IShow) => {
    const currentlyIncluded = show.excludeFromTranslation === 'false'
    const newIncludeState = !currentlyIncluded
    await showStore.include(MEDIA_TYPE.SHOW, show.id, newIncludeState)
}

const handleIncludeAll = async () => {
    const newState = !showStore.includeSummary.allIncluded
    await showStore.includeAll(newState)
}

onMounted(async () => {
    await showStore.fetch()
})
</script>
