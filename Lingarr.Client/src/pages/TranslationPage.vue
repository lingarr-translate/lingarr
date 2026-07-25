<template>
    <div class="w-full">
        <!-- Search and Filters -->
        <div class="flex flex-wrap items-center justify-between gap-2 bg-tertiary p-4">
            <SearchComponent v-model="filter" />
            <div
                class="flex w-full flex-col gap-2 md:w-fit md:flex-row md:justify-between md:space-x-2">
                <button
                    v-if="isSelectMode"
                    class="hover:text-primary-content/50 cursor-pointer rounded-md border border-accent px-2 py-1 text-primary-content transition-colors"
                    @click="handleDelete">
                    Delete ({{ translationRequestStore.selectedRequests.length }})
                </button>
                <button
                    class="hover:text-primary-content/50 cursor-pointer rounded-md border border-accent px-2 py-1 text-primary-content transition-colors"
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

        <div class="w-full px-4">
            <div class="hidden border-b border-accent font-bold md:grid md:grid-cols-12">
                <div class="col-span-4 px-4 py-2">Title</div>
                <div class="col-span-1 px-4 py-2">Source</div>
                <div class="col-span-1 px-4 py-2">Target</div>
                <div class="col-span-1 px-4 py-2">Status</div>
                <div class="px-4 py-2" :class="isSelectMode ? 'col-span-2' : 'col-span-3'">
                    Progress
                </div>
                <div class="col-span-1 px-4 py-2">Completed</div>
                <div class="col-span-1"></div>
                <div
                    v-if="isSelectMode"
                    class="col-span-1 flex items-center justify-center px-4 py-2">
                    <CheckboxComponent
                        :model-value="translationRequestStore.selectAll"
                        @change="translationRequestStore.toggleSelectAll()" />
                </div>
            </div>
            <div
                v-for="item in translationRequests.items"
                :key="item.id"
                class="border-accent hover:bg-accent/5 flex flex-wrap items-center gap-x-3 gap-y-2 border-b py-3 transition-colors md:grid md:grid-cols-12 md:gap-0 md:py-0">
                <div class="flex w-full items-center gap-2 md:col-span-4 md:w-auto md:px-4 md:py-2">
                    <span
                        v-if="item.mediaType === MEDIA_TYPE.EPISODE"
                        v-show-title
                        class="block min-w-0 flex-1 cursor-help md:flex-none"
                        :title="item.title">
                        {{ item.title }}
                    </span>
                    <span v-else class="min-w-0 flex-1 md:flex-none">
                        {{ item.title }}
                    </span>
                    <span class="ml-auto flex flex-none items-center gap-2 md:hidden">
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
                <div class="flex items-center md:col-span-1 md:px-4 md:py-2">
                    <BadgeComponent classes="text-primary-content border-accent bg-secondary">
                        {{ item.sourceLanguage.toUpperCase() }}
                    </BadgeComponent>
                </div>
                <div class="flex items-center gap-2 md:col-span-1 md:px-4 md:py-2">
                    <span class="text-primary-content/50 md:hidden">→</span>
                    <BadgeComponent classes="text-primary-content border-accent bg-secondary">
                        {{ item.targetLanguage.toUpperCase() }}
                    </BadgeComponent>
                </div>
                <div class="flex items-center md:col-span-1 md:px-4 md:py-2">
                    <TranslationStatus :translation-status="item.status" />
                </div>
                <div
                    class="items-center md:flex md:px-4 md:py-2"
                    :class="[
                        isSelectMode ? 'md:col-span-2' : 'md:col-span-3',
                        item.status === TRANSLATION_STATUS.INPROGRESS && item.progress
                            ? 'flex w-full md:w-auto'
                            : 'hidden'
                    ]">
                    <div
                        v-if="item.status === TRANSLATION_STATUS.INPROGRESS && item.progress"
                        class="w-full">
                        <TranslationProgress :progress="item.progress" />
                    </div>
                </div>
                <div
                    :class="item.completedAt ? 'flex' : 'hidden md:flex'"
                    class="items-center gap-2 md:col-span-1 md:px-4 md:py-2">
                    <TranslationCompletedAt
                        v-if="item.completedAt"
                        :completed-at="item.completedAt" />
                    <span
                        class="text-primary-content/50 text-xs font-semibold tracking-wide uppercase md:hidden">
                        Completed
                    </span>
                </div>
                <div
                    class="hidden items-center justify-between md:col-span-1 md:flex md:justify-end md:py-2">
                    <div class="flex items-center justify-center gap-1">
                        <TranslationAction
                            :item="item"
                            :on-action="(action) => handleAction(item, action)" />
                    </div>
                </div>
                <div
                    v-if="isSelectMode"
                    class="hidden items-center justify-center py-2 md:col-span-1 md:flex md:px-4">
                    <CheckboxComponent
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
    MEDIA_TYPE,
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
        default:
            console.error('unknown translation request action: ' + action)
    }
}

onMounted(async () => {
    await translationRequestStore.fetch()
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
