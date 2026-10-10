<template>
    <div class="media-library w-full">
        <div class="media-toolbar">
            <div class="translation-toolbar-query">
                <SearchComponent v-model="filter" />
                <div class="translation-toolbar-controls">
                    <ButtonComponent size="sm" variant="secondary" @click="toggleSelectMode">
                        {{ isSelectMode ? 'Cancel' : 'Select' }}
                    </ButtonComponent>
                    <div class="media-toolbar-sort min-w-0 flex-1">
                        <SortControls
                            v-model="filter"
                            :options="[
                                { label: 'Sort by Added', value: 'CreatedAt' },
                                { label: 'Sort by Completed', value: 'CompletedAt' },
                                { label: 'Sort by Title', value: 'Title' }
                            ]" />
                        <ReloadComponent @toggle:update="translationRequestStore.fetch()" />
                    </div>
                </div>
            </div>
            <div v-if="isSelectMode" class="media-toolbar-actions">
                <CheckboxComponent
                    label="Select all on this page"
                    :model-value="translationRequestStore.selectAll"
                    @change="translationRequestStore.toggleSelectAll()" />
                <ButtonComponent
                    size="sm"
                    variant="accent"
                    :disabled="translationRequestStore.selectedRequests.length === 0"
                    @click="handleDelete">
                    Delete ({{ translationRequestStore.selectedRequests.length }})
                </ButtonComponent>
            </div>
        </div>

        <div class="translation-list mx-4 sm:mx-6" :class="{ 'is-selecting': isSelectMode }">
            <div class="translation-heading media-table-heading border-b font-semibold">
                <div>Translation</div>
                <div>Status / progress</div>
                <div class="text-right">Actions</div>
            </div>
            <div
                v-for="item in translationRequests.items"
                :key="item.id"
                class="translation-row media-row border-b transition-colors">
                <div class="translation-identity">
                    <div class="translation-title font-medium">{{ item.title }}</div>
                    <div
                        class="translation-languages flex flex-wrap items-center gap-2"
                        :aria-label="`From ${item.sourceLanguage.toUpperCase()} to ${item.targetLanguage.toUpperCase()}`">
                        <BadgeComponent classes="text-primary-content ui-border bg-secondary">
                            <span class="sr-only">Source:</span>
                            {{ item.sourceLanguage.toUpperCase() }}
                        </BadgeComponent>
                        <span class="text-secondary-content" aria-hidden="true">→</span>
                        <BadgeComponent classes="text-primary-content ui-border bg-secondary">
                            <span class="sr-only">Target:</span>
                            {{ item.targetLanguage.toUpperCase() }}
                        </BadgeComponent>
                    </div>
                </div>
                <div class="translation-state">
                    <TranslationStatus :translation-status="item.status" class="font-medium" />
                    <div
                        v-if="item.status === TRANSLATION_STATUS.INPROGRESS"
                        class="translation-progress"
                        role="progressbar"
                        :aria-label="`Translation progress for ${item.title}`"
                        :aria-valuenow="item.progress ?? 0"
                        :aria-valuemin="0"
                        :aria-valuemax="100">
                        <TranslationProgress :progress="item.progress ?? 0" />
                        <span class="text-secondary-content text-xs tabular-nums">
                            {{ item.progress ?? 0 }}%
                        </span>
                    </div>
                    <div v-if="item.completedAt" class="text-secondary-content text-xs">
                        <span class="sr-only">Completed:</span>
                        <TranslationCompletedAt :completed-at="item.completedAt" />
                    </div>
                </div>
                <div class="translation-actions">
                    <TranslationAction
                        :item="item"
                        :on-action="(action) => handleAction(item, action)" />
                    <CheckboxComponent
                        v-if="isSelectMode"
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
import ButtonComponent from '@/components/common/ButtonComponent.vue'

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

<style scoped>
.translation-toolbar-query {
    display: grid;
    align-items: center;
    gap: 0.75rem;
}
.translation-toolbar-controls {
    display: flex;
    align-items: center;
    gap: 0.75rem;
    min-width: 0;
}
.translation-toolbar-controls > button {
    flex-shrink: 0;
}
.translation-list {
    container: translations / inline-size;
    --translation-actions-width: 12rem;
}
.translation-list.is-selecting {
    --translation-actions-width: 15rem;
}
.translation-heading {
    display: none;
}
.translation-row {
    display: grid;
    grid-template-columns: minmax(0, 1fr);
    grid-template-areas: 'identity' 'state' 'actions';
    align-items: center;
    gap: 0.75rem;
    padding: 1rem 0;
}
.translation-identity {
    grid-area: identity;
    display: grid;
    gap: 0.375rem;
}
.translation-title {
    overflow-wrap: anywhere;
}
.translation-state {
    grid-area: state;
    display: grid;
    gap: 0.25rem;
    align-content: center;
}
.translation-progress {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    width: 100%;
    max-width: 14rem;
}
.translation-actions {
    grid-area: actions;
    display: flex;
    align-items: center;
    gap: 0.25rem;
}
@container media-library (min-width: 42rem) {
    .translation-toolbar-query {
        grid-template-columns: minmax(0, 1fr) auto;
    }
}
@container translations (min-width: 28rem) {
    .translation-row {
        grid-template-columns: minmax(0, 1fr) auto;
        grid-template-areas: 'identity identity' 'state actions';
    }
    .translation-actions {
        justify-content: flex-end;
    }
}
@container translations (min-width: 42rem) {
    .translation-heading,
    .translation-row {
        display: grid;
        grid-template-columns: minmax(0, 1fr) 9rem var(--translation-actions-width);
        grid-template-areas: 'identity state actions';
        align-items: center;
        gap: 1rem;
        padding: 0.75rem;
    }
    .translation-row {
        min-height: 4.5rem;
    }
}
</style>
