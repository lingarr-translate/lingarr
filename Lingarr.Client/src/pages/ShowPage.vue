<template>
    <div v-if="settingsCompleted === 'true'" class="w-full">
        <div class="bg-tertiary flex flex-wrap items-center justify-between gap-2 p-4">
            <SearchComponent v-model="filter" />
            <div
                class="flex w-full flex-col gap-2 md:w-fit md:flex-row md:justify-between md:space-x-2">
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
            <div class="bg-primary text-primary-content mb-2 flex flex-wrap items-center justify-between gap-3 rounded-md px-4 py-3">
                <div class="flex items-center gap-3">
                    <ToggleButton
                        :model-value="includeAllShows"
                        size="small"
                        @toggle:update="onToggleIncludeAllShows" />
                    <div class="leading-tight">
                        <div class="font-semibold">
                            {{ translate('tvShows.includeAll') }}
                        </div>
                        <div class="text-sm opacity-80">
                            {{ translate('tvShows.excludedLabel') }}:
                            {{ includeSummary?.excludedShows ?? 0 }} / {{ includeSummary?.totalShows ?? 0 }}
                        </div>
                    </div>
                </div>
            </div>
            <!-- Shows -->
            <div class="border-accent grid grid-cols-12 border-b font-bold">
                <div class="col-span-8 px-4 py-2">{{ translate('tvShows.title') }}</div>
                <div class="col-span-1 px-4 py-2">
                    <span class="hidden md:block">
                        {{ translate('tvShows.include') }}
                    </span>
                    <span class="block md:hidden">⊘</span>
                </div>
                <div class="col-span-1 px-4 py-2">
                    {{ translate('tvShows.ageThreshold') }}
                </div>
                <div class="col-span-2 flex justify-end px-4 py-2">
                    <ReloadComponent @toggle:update="onReload" />
                </div>
            </div>
            <div v-for="item in shows.items" :key="item.id">
                <div
                    class="border-accent grid cursor-pointer grid-cols-12 border-b"
                    @click="toggleShow(item)">
                    <div class="col-span-8 flex items-center px-4 py-2">
                        <CaretButton :is-expanded="expandedShow !== item.id" class="pr-2" />
                        {{ item.title }}
                    </div>
                    <div class="col-span-1 flex items-center px-4 py-2" @click.stop>
                        <ToggleButton
                            :model-value="!item.excludeFromTranslation"
                            size="small"
                            @toggle:update="() => onToggleIncludeShow(item)" />
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
                </div>
                <SeasonTable
                    v-if="expandedShow === item.id"
                    :seasons="item.seasons"
                    @include-changed="refreshIncludeSummary" />
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
import { IFilter, IIncludeSummary, IPagedResult, IShow, MEDIA_TYPE, SETTINGS } from '@/ts'
import useDebounce from '@/composables/useDebounce'
import { useInstanceStore } from '@/store/instance'
import { useSettingStore } from '@/store/setting'
import { useShowStore } from '@/store/show'
import PaginationComponent from '@/components/common/PaginationComponent.vue'
import SearchComponent from '@/components/common/SearchComponent.vue'
import CaretButton from '@/components/common/CaretButton.vue'
import SortControls from '@/components/common/SortControls.vue'
import ReloadComponent from '@/components/common/ReloadComponent.vue'
import NoMediaNotification from '@/components/common/NoMediaNotification.vue'
import ToggleButton from '@/components/common/ToggleButton.vue'
import SeasonTable from '@/components/features/show/SeasonTable.vue'
import InputComponent from '@/components/common/InputComponent.vue'

const instanceStore = useInstanceStore()
const showStore = useShowStore()
const settingStore = useSettingStore()
const expandedShow: Ref<boolean | number | null> = ref(null)
const includeSummary: Ref<IIncludeSummary | null> = ref(null)
const includeAllShows: Ref<boolean> = ref(true)

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

const refreshIncludeSummary = async () => {
    includeSummary.value = await showStore.fetchIncludeSummary()
    includeAllShows.value = includeSummary.value
        ? includeSummary.value.excludedShows === 0
        : true
}

const onToggleIncludeAllShows = async () => {
    const nextInclude = !includeAllShows.value
    includeAllShows.value = nextInclude
    await showStore.includeAll(nextInclude)
    await showStore.fetch()
    await refreshIncludeSummary()
}

const onToggleIncludeShow = async (show: IShow) => {
    const newIncludeState = show.excludeFromTranslation // if currently excluded, new state is included
    show.excludeFromTranslation = !newIncludeState
    await showStore.include(MEDIA_TYPE.SHOW, show.id, newIncludeState)
    await refreshIncludeSummary()
}

const onReload = async () => {
    await showStore.fetch()
    await refreshIncludeSummary()
}

const toggleShow = async (show: IShow) => {
    if (expandedShow.value === show.id) {
        expandedShow.value = null
        return
    }
    instanceStore.setPoster({ content: show, type: 'show' })
    expandedShow.value = show.id
}
onMounted(async () => {
    await showStore.fetch()
    await refreshIncludeSummary()
})
</script>
