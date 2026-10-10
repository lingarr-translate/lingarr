<template>
    <div class="flex flex-wrap items-center gap-1">
        <button
            type="button"
            class="ui-icon-button"
            aria-label="View translation details"
            v-tooltip="'View translation details'"
            @click="router.push({ name: 'translation-detail', params: { id: item.id } })">
            <EyeOnIcon class="h-5 w-5" />
        </button>
        <div class="flex flex-wrap items-center gap-1">
            <LoaderCircleIcon v-if="loading" class="h-5 w-5 animate-spin" />
            <button
                v-else-if="inProgress"
                type="button"
                class="ui-icon-button"
                aria-label="Cancel translation"
                v-tooltip="'Cancel translation'"
                @click="executeAction(TRANSLATION_ACTIONS.CANCEL)">
                <TimesIcon class="h-5 w-5" />
            </button>
            <template v-else-if="removable">
                <button
                    v-if="resumable"
                    :disabled="loading"
                    aria-label="Resume translation"
                    v-tooltip="'Resume, reuse already-translated lines'"
                    type="button"
                    class="ui-icon-button"
                    @click="executeAction(TRANSLATION_ACTIONS.RESUME)">
                    <StartIcon class="h-5 w-5" />
                </button>
                <button
                    v-if="proofreadable"
                    :disabled="loading"
                    aria-label="Proofread translation"
                    v-tooltip="'Proofread, compare against the source and correct mistranslations'"
                    type="button"
                    class="ui-icon-button"
                    @click="executeAction(TRANSLATION_ACTIONS.PROOFREAD)">
                    <SearchIcon class="h-5 w-5" />
                </button>
                <button
                    :disabled="loading"
                    aria-label="Retry translation"
                    v-tooltip="'Retry, start over'"
                    type="button"
                    class="ui-icon-button"
                    @click="executeAction(TRANSLATION_ACTIONS.RETRY)">
                    <RetryIcon class="h-5 w-5" />
                </button>
                <button
                    :disabled="loading"
                    type="button"
                    class="ui-icon-button"
                    aria-label="Remove translation request"
                    v-tooltip="'Remove translation request'"
                    @click="executeAction(TRANSLATION_ACTIONS.REMOVE)">
                    <TrashIcon class="h-5 w-5" />
                </button>
            </template>
        </div>
    </div>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue'
import { useRouter } from 'vue-router'
import { ITranslationRequest, TRANSLATION_ACTIONS, TRANSLATION_STATUS } from '@/ts'
import useTranslationRequestStore from '@/store/translationRequest'
import TimesIcon from '@/components/icons/TimesIcon.vue'
import LoaderCircleIcon from '@/components/icons/LoaderCircleIcon.vue'
import TrashIcon from '@/components/icons/TrashIcon.vue'
import RetryIcon from '@/components/icons/RetryIcon.vue'
import StartIcon from '@/components/icons/StartIcon.vue'
import SearchIcon from '@/components/icons/SearchIcon.vue'
import EyeOnIcon from '@/components/icons/EyeOnIcon.vue'
const router = useRouter()
const translationRequestStore = useTranslationRequestStore()

const props = defineProps<{
    item: ITranslationRequest
    onAction: (action: TRANSLATION_ACTIONS) => Promise<void>
}>()

const loading = ref(false)

const inProgress = computed(
    () =>
        props.item.status === TRANSLATION_STATUS.INPROGRESS ||
        props.item.status === TRANSLATION_STATUS.PENDING
)

const removable = computed(
    () =>
        props.item.status !== TRANSLATION_STATUS.PENDING &&
        props.item.status !== TRANSLATION_STATUS.INPROGRESS
)

const resumable = computed(
    () =>
        props.item.status === TRANSLATION_STATUS.FAILED ||
        props.item.status === TRANSLATION_STATUS.CANCELLED ||
        props.item.status === TRANSLATION_STATUS.INTERRUPTED
)

const proofreadable = computed(
    () =>
        props.item.status === TRANSLATION_STATUS.COMPLETED &&
        !!props.item.translatedSubtitle &&
        translationRequestStore.proofreadSupported
)

const executeAction = async (action: TRANSLATION_ACTIONS) => {
    loading.value = true
    try {
        await props.onAction(action)
    } catch {
        loading.value = false
        return
    }

    if (action == TRANSLATION_ACTIONS.RETRY || action == TRANSLATION_ACTIONS.PROOFREAD) {
        // Special case to set loading to false when onAction is done
        // Retry and Proofread are the only cases that do not reliably change props.status right away
        loading.value = false
    }
}

watch(
    () => props.item.status,
    () => {
        loading.value = false
    }
)
</script>
