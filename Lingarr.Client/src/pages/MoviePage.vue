<template>
    <PageLayout>
        <div v-if="settingsCompleted === 'true'" class="w-full">
            <div class="flex flex-wrap items-center justify-between gap-2 bg-tertiary p-4">
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
                <div class="grid grid-cols-12 border-b border-accent font-bold">
                    <div class="col-span-6 px-4 py-2">{{ translate('movies.title') }}</div>
                    <div class="col-span-4 px-4 py-2">{{ translate('movies.subtitles') }}</div>
                    <div class="col-span-2 flex justify-end px-4 py-2">
                        <ReloadComponent @toggle:update="movieStore.fetch()" />
                    </div>
                </div>
                <div v-for="item in movies.items" :key="item.id">
                    <div class="grid grid-cols-12 border-b border-accent">
                        <div class="col-span-6 px-4 py-2">
                            {{ item.title }}
                        </div>
                        <div class="col-span-6 flex items-center gap-2 px-4 py-2">
                            <ContextMenu
                                v-for="(subtitle, index) in item.subtitles"
                                :key="`${index}-${subtitle.fileName}`"
                                :subtitle="subtitle"
                                :media="item"
                                :media-type="MEDIA_TYPE.MOVIE"
                                @update:toggle="toggleMovie(item)">
                                <BadgeComponent>
                                    {{ subtitle.language.toUpperCase() }}
                                </BadgeComponent>
                            </ContextMenu>
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
    </PageLayout>
</template>

<script setup lang="ts">
import { computed, onMounted, ComputedRef } from 'vue'
import { IFilter, IMovie, IPagedResult, MEDIA_TYPE, SETTINGS } from '@/ts'
import useDebounce from '@/composables/useDebounce'
import { useMovieStore } from '@/store/movie'
import { useSettingStore } from '@/store/setting'
import { useInstanceStore } from '@/store/instance'
import PaginationComponent from '@/components/common/PaginationComponent.vue'
import PageLayout from '@/components/layout/PageLayout.vue'
import BadgeComponent from '@/components/common/BadgeComponent.vue'
import SortControls from '@/components/common/SortControls.vue'
import SearchComponent from '@/components/common/SearchComponent.vue'
import ContextMenu from '@/components/layout/ContextMenu.vue'
import ReloadComponent from '@/components/common/ReloadComponent.vue'
import NoMediaNotification from '@/components/common/NoMediaNotification.vue'

const movieStore = useMovieStore()
const settingStore = useSettingStore()
const instanceStore = useInstanceStore()

const settingsCompleted: ComputedRef<string> = computed(
    () => settingStore.getSetting(SETTINGS.RADARR_SETTINGS_COMPLETED) as string
)
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
