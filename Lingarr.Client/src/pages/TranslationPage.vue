<template>
    <PageLayout>
        <div class="w-full">
            <!-- Search and Filters -->
            <div class="flex flex-wrap items-center justify-between gap-2 bg-tertiary p-4">
                <SearchComponent v-model="filter" />
                <div class="flex w-full justify-between space-x-2 md:w-fit">
                    <SortControls
                        v-model="filter"
                        :options="[
                            {
                                label: 'Sort by Added',
                                value: 'CreatedAt'
                            },
                            {
                                label: 'Sort by Completed',
                                value: 'CompletedAt'
                            },
                            {
                                label: 'Sort by Title',
                                value: 'Title'
                            }
                        ]" />
                </div>
            </div>

            <!-- Media List View -->
            <div class="w-full px-4">
                <div class="hidden border-b border-accent font-bold md:grid md:grid-cols-12">
                    <div class="col-span-3 px-4 py-2">Title</div>
                    <div class="col-span-1 px-4 py-2">Source</div>
                    <div class="col-span-1 px-4 py-2">Target</div>
                    <div class="col-span-1 px-4 py-2">Status</div>
                    <div class="col-span-4 px-4 py-2">Progress</div>
                    <div class="col-span-1 px-4 py-2">Completed</div>
                    <div class="col-span-1 flex justify-end px-4 py-2">
                        <ReloadComponent @toggle:update="translationRequestStore.fetch()" />
                    </div>
                </div>
                <div
                    v-for="item in translationRequests.items"
                    :key="item.id"
                    class="rounded-lg py-4 shadow md:grid md:grid-cols-12 md:rounded-none md:border-b md:border-accent md:bg-transparent md:p-0 md:shadow-none">
                    <div class="deletable float-right h-5 w-5 md:hidden">
                        <TranslationDeletable
                            :item="item"
                            :progress-map="requestProgressMap"
                            @toggle:remove="remove(item)" />
                    </div>
                    <div class="mb-2 md:col-span-3 md:mb-0 md:px-4 md:py-2">
                        <span :id="`deletable-${item.id}`" class="font-bold md:hidden">
                            Title:&nbsp;
                        </span>
                        <span v-if="item.mediaType === MEDIA_TYPE.EPISODE">
                            {{ item.media.showTitle }}:&nbsp;
                        </span>
                        <span
                            :class="{
                                'text-primary-content/50': item.mediaType === MEDIA_TYPE.EPISODE
                            }">
                            {{ item.media.title }}
                        </span>
                    </div>
                    <div class="mb-2 md:col-span-1 md:mb-0 md:px-4 md:py-2">
                        <span class="font-bold md:hidden">Source:&nbsp;</span>
                        <BadgeComponent classes="text-primary-content border-accent bg-secondary">
                            {{ item.sourceLanguage.toUpperCase() }}
                        </BadgeComponent>
                    </div>
                    <div class="mb-2 md:col-span-1 md:mb-0 md:px-4 md:py-2">
                        <span class="font-bold md:hidden">Target:&nbsp;</span>
                        <BadgeComponent classes="text-primary-content border-accent bg-secondary">
                            {{ item.targetLanguage.toUpperCase() }}
                        </BadgeComponent>
                    </div>
                    <div class="mb-2 md:col-span-1 md:mb-0 md:px-4 md:py-2">
                        <span class="font-bold md:hidden">Status:&nbsp;</span>
                        <TranslationStatus :item="item" :progress-map="requestProgressMap" />
                    </div>
                    <div class="mb-2 flex items-center md:col-span-4 md:mb-0 md:px-4 md:py-2">
                        <div
                            v-if="
                                item?.status !== TRANSLATION_STATUS.COMPLETED &&
                                item?.status !== TRANSLATION_STATUS.CANCELLED
                            "
                            class="w-full">
                            <span class="mr-2 font-bold md:hidden">Progress:&nbsp;</span>
                            <TranslationProgress :id="item.id" :progress-map="requestProgressMap" />
                        </div>
                    </div>
                    <div class="mb-2 md:col-span-1 md:mb-0 md:px-4 md:py-2">
                        <span class="font-bold md:hidden">Completed:&nbsp;</span>
                        <TranslationCompletedAt :item="item" :progress-map="requestProgressMap" />
                    </div>
                    <div
                        class="hidden items-center justify-between md:col-span-1 md:flex md:justify-end md:px-4 md:py-2">
                        <div class="flex h-5 w-5 items-center justify-center">
                            <TranslationDeletable
                                :item="item"
                                :progress-map="requestProgressMap"
                                @toggle:remove="remove(item)" />
                        </div>
                    </div>
                </div>
            </div>
            <PaginationComponent
                v-if="translationRequests.totalCount"
                v-model="filter"
                :total-count="translationRequests.totalCount"
                :page-size="translationRequests.pageSize" />
        </div>
    </PageLayout>
</template>

<script setup lang="ts">
import { onMounted, onUnmounted, ComputedRef, computed } from 'vue'
import {
    IFilter,
    IPagedResult,
    IProgressMap,
    ITranslationRequest,
    MEDIA_TYPE,
    TRANSLATION_STATUS
} from '@/ts'
import { useTranslationRequestStore } from '@/store/translationRequest'
import { useSignalR } from '@/composables/useSignalR'
import useDebounce from '@/composables/useDebounce'
import PaginationComponent from '@/components/common/PaginationComponent.vue'
import SortControls from '@/components/common/SortControls.vue'
import SearchComponent from '@/components/common/SearchComponent.vue'
import ReloadComponent from '@/components/common/ReloadComponent.vue'
import TranslationStatus from '@/components/common/TranslationStatus.vue'
import TranslationProgress from '@/components/common/TranslationProgress.vue'
import TranslationDeletable from '@/components/common/TranslationDeletable.vue'
import TranslationCompletedAt from '@/components/common/TranslationCompletedAt.vue'
import BadgeComponent from '@/components/common/BadgeComponent.vue'
import PageLayout from '@/components/layout/PageLayout.vue'

const signalR = useSignalR()
const translationRequestStore = useTranslationRequestStore()

const translationRequests: ComputedRef<IPagedResult<ITranslationRequest>> = computed(
    () => translationRequestStore.getTranslationRequests
)

const requestProgressMap: ComputedRef<IProgressMap> = computed(
    () => translationRequestStore.progressMap
)

const filter: ComputedRef<IFilter> = computed({
    get: () => translationRequestStore.filter,
    set: useDebounce((value: IFilter) => {
        translationRequestStore.setFilter(value)
    }, 300)
})

function remove(translationRequest: ITranslationRequest) {
    translationRequestStore.cancel(translationRequest)
}

onMounted(async () => {
    await translationRequestStore.fetch()
    const translationRequest = await signalR.connect(
        'TranslationRequests',
        '/signalr/TranslationRequests'
    )

    await translationRequest.joinGroup({ group: 'TranslationRequests' })
    translationRequest.on('RequestProgress', translationRequestStore.updateProgress)
})
onUnmounted(async () => {
    const translationRequest = await signalR.connect(
        'TranslationRequests',
        '/signalr/TranslationRequest'
    )
    translationRequest.off('RequestProgress', translationRequestStore.updateProgress)
})
</script>
