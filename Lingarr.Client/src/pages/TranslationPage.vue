<template>
    <div class="w-full">
        <!-- Search and Filters -->
        <div class="ui-toolbar">
            <SearchComponent v-model="filter" />
            <div class="flex w-full flex-wrap items-center gap-2 sm:w-auto">
                <button
                    v-if="isSelectMode"
                    type="button"
                    class="ui-border hover:bg-accent/10 min-h-11 cursor-pointer rounded-lg border px-4 py-2 text-sm font-medium"
                    @click="handleDelete">
                    Delete ({{ translationRequestStore.selectedRequests.length }})
                </button>
                <button
                    type="button"
                    class="ui-border hover:bg-accent/10 min-h-11 cursor-pointer rounded-lg border px-4 py-2 text-sm font-medium"
                    @click="toggleSelectMode">
                    {{ isSelectMode ? 'Cancel' : 'Select' }}
                </button>
                <div class="flex flex-wrap items-center gap-2">
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
                    <ReloadComponent @toggle:update="translationRequestStore.fetch()" />
                </div>
            </div>
        </div>

        <div class="w-full px-4 sm:px-6">
            <div class="media-table-heading hidden border-b font-semibold xl:grid xl:grid-cols-12">
                <div class="col-span-3 px-3 py-2">Title</div>
                <div class="col-span-1 px-4 py-2">Source</div>
                <div class="col-span-1 px-4 py-2">Target</div>
                <div class="col-span-1 px-4 py-2">Status</div>
                <div class="px-4 py-2" :class="isSelectMode ? 'col-span-1' : 'col-span-2'">
                    Progress
                </div>
                <div class="col-span-1 px-4 py-2">Completed</div>
                <div class="col-span-3 px-3 py-2 text-right">Actions</div>
                <div
                    v-if="isSelectMode"
                    class="col-span-1 flex items-center justify-center px-4 py-2">
                    <CheckboxComponent
                        aria-label="Select all items on this page"
                        :model-value="translationRequestStore.selectAll"
                        @change="translationRequestStore.toggleSelectAll()" />
                </div>
            </div>
            <div
                v-for="item in translationRequests.items"
                :key="item.id"
                class="media-row hover:bg-accent/5 flex flex-wrap items-center gap-x-3 gap-y-2 border-b py-3 transition-colors xl:grid xl:grid-cols-12 xl:gap-0 xl:py-2">
                <div
                    class="flex w-full flex-wrap items-center gap-2 xl:col-span-3 xl:w-auto xl:px-2 xl:py-2">
                    <span class="min-w-0 flex-1 basis-full font-medium xl:basis-auto">
                        {{ item.title }}
                    </span>
                    <span class="flex w-full flex-wrap items-center gap-2 xl:hidden">
                        <TranslationAction
                            :item="item"
                            :on-action="(action) => handleAction(item, action)" />
                        <CheckboxComponent
                            v-if="isSelectMode"
                            :model-value="
                                translationRequestStore.selectedRequests.some(
                                    (request) => request.id === item.id
                                )
                            "
                            @change="translationRequestStore.toggleSelect(item)" />
                    </span>
                </div>
                <div class="flex items-center xl:col-span-1 xl:px-2 xl:py-2">
                    <BadgeComponent classes="text-primary-content ui-border bg-secondary">
                        {{ item.sourceLanguage.toUpperCase() }}
                    </BadgeComponent>
                </div>
                <div class="flex items-center gap-2 xl:col-span-1 xl:px-2 xl:py-2">
                    <span class="text-secondary-content xl:hidden">→</span>
                    <BadgeComponent classes="text-primary-content ui-border bg-secondary">
                        {{ item.targetLanguage.toUpperCase() }}
                    </BadgeComponent>
                </div>
                <div class="flex items-center xl:col-span-1 xl:px-2 xl:py-2">
                    <TranslationStatus :translation-status="item.status" />
                </div>
                <div
                    class="items-center xl:flex xl:px-2 xl:py-2"
                    :class="[
                        isSelectMode ? 'xl:col-span-1' : 'xl:col-span-2',
                        item.status === TRANSLATION_STATUS.INPROGRESS && item.progress
                            ? 'flex w-full xl:w-auto'
                            : 'hidden'
                    ]">
                    <div
                        v-if="item.status === TRANSLATION_STATUS.INPROGRESS && item.progress"
                        class="w-full">
                        <TranslationProgress :progress="item.progress" />
                    </div>
                </div>
                <div
                    :class="item.completedAt ? 'flex' : 'hidden xl:flex'"
                    class="items-center gap-2 xl:col-span-1 xl:px-2 xl:py-2">
                    <TranslationCompletedAt
                        v-if="item.completedAt"
                        :completed-at="item.completedAt" />
                    <span
                        class="text-secondary-content text-xs font-semibold tracking-wide uppercase xl:hidden">
                        Completed
                    </span>
                </div>
                <div
                    class="hidden items-center justify-between xl:col-span-3 xl:flex xl:justify-end xl:py-2">
                    <div class="flex items-center justify-center gap-1">
                        <TranslationAction
                            :item="item"
                            :on-action="(action) => handleAction(item, action)" />
                    </div>
                </div>
                <div
                    v-if="isSelectMode"
                    class="hidden items-center justify-center py-2 xl:col-span-1 xl:flex xl:px-2">
                    <CheckboxComponent
                        :aria-label="`Select ${item.title}`"
                        :model-value="
                            translationRequestStore.selectedRequests.some(
                                (request) => request.id === item.id
                            )
                        "
                        @change="translationRequestStore.toggleSelect(item)" />
                </div>
            </div>
        </div>
        <PaginationComponent
            v-if="translationRequests.totalCount"
            v-model="filter"
            :total-count="translationRequests.totalCount"
            :page-size="translationRequests.pageSize" />
    </div>
</template>

<script setup lang="ts">
import { ref, onMounted, onUnmounted, ComputedRef, computed } from 'vue'
import {
    Hub,
    IFilter,
    IPagedResult,
    ITranslationRequest,
    TRANSLATION_ACTIONS,
    TRANSLATION_STATUS
} from '@/ts'
import useTranslationRequestStore from '@/store/translationRequest'
import { useSignalR } from '@/composables/useSignalR'
import useDebounce from '@/composables/useDebounce'
import PaginationComponent from '@/components/common/PaginationComponent.vue'
import SortControls from '@/components/common/SortControls.vue'
import SearchComponent from '@/components/common/SearchComponent.vue'
import ReloadComponent from '@/components/common/ReloadComponent.vue'
import TranslationStatus from '@/components/common/TranslationStatus.vue'
import TranslationProgress from '@/components/common/TranslationProgress.vue'
import TranslationAction from '@/components/common/TranslationAction.vue'
import TranslationCompletedAt from '@/components/common/TranslationCompletedAt.vue'
import BadgeComponent from '@/components/common/BadgeComponent.vue'
import CheckboxComponent from '@/components/common/CheckboxComponent.vue'

const signalR = useSignalR()
const hubConnection = ref<Hub>()
const translationRequestStore = useTranslationRequestStore()

const translationRequests: ComputedRef<IPagedResult<ITranslationRequest>> = computed(
    () => translationRequestStore.getTranslationRequests
)

const filter: ComputedRef<IFilter> = computed({
    get: () => translationRequestStore.filter,
    set: useDebounce((value: IFilter) => {
        translationRequestStore.setFilter(value)
    }, 300)
})

async function handleAction(translationRequest: ITranslationRequest, action: TRANSLATION_ACTIONS) {
    switch (action) {
        case TRANSLATION_ACTIONS.CANCEL:
            return await translationRequestStore.cancel(translationRequest)
        case TRANSLATION_ACTIONS.REMOVE:
            return await translationRequestStore.remove(translationRequest)
        case TRANSLATION_ACTIONS.RETRY:
            return await translationRequestStore.retry(translationRequest)
        case TRANSLATION_ACTIONS.RESUME:
            return await translationRequestStore.resume(translationRequest)
        case TRANSLATION_ACTIONS.PROOFREAD:
            return await translationRequestStore.proofread(translationRequest)
        default:
            console.error('unknown translation request action: ' + action)
    }
}

onMounted(async () => {
    await translationRequestStore.fetch()
    await translationRequestStore.fetchProofreadStatus()
    hubConnection.value = await signalR.connect(
        'TranslationRequests',
        '/signalr/TranslationRequests'
    )

    await hubConnection.value.joinGroup({ group: 'TranslationRequests' })
    hubConnection.value.on('RequestProgress', translationRequestStore.updateProgress)
})

onUnmounted(async () => {
    hubConnection.value?.off('RequestProgress', translationRequestStore.updateProgress)
})

const isSelectMode = ref(false)

const toggleSelectMode = () => {
    isSelectMode.value = !isSelectMode.value
    if (!isSelectMode.value) {
        translationRequestStore.clearSelection()
    }
}

const handleDelete = async () => {
    for (const request of translationRequestStore.getSelectedRequests) {
        await translationRequestStore.remove(request)
    }
    translationRequestStore.clearSelection()
    translationRequestStore.fetch()
}
</script>
