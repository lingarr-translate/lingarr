<template>
    <button :disabled="loading">
        <LoaderCircleIcon v-if="loading" class="h-5 w-5 animate-spin" />
        <TimesIcon v-else-if="inProgress" @click="executeAction(TRANSLATION_ACTIONS.CANCEL)" class="h-5 w-5 cursor-pointer" />
        <div v-else-if="removable" class="flex space-x-2">
            <ReloadIcon @click="executeAction(TRANSLATION_ACTIONS.RETRY)" class="h-5 w-5 cursor-pointer" />
            <TrashIcon @click="executeAction(TRANSLATION_ACTIONS.REMOVE)" class="h-5 w-5 cursor-pointer" />
        </div>
    </button>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue'
import { TRANSLATION_ACTIONS, TRANSLATION_STATUS, TranslationStatus } from '@/ts'
import TimesIcon from '@/components/icons/TimesIcon.vue'
import LoaderCircleIcon from '@/components/icons/LoaderCircleIcon.vue'
import TrashIcon from '@/components/icons/TrashIcon.vue'
import ReloadIcon from '@/components/icons/ReloadIcon.vue'

const props = defineProps<{
    status: TranslationStatus,
    onAction: (action: TRANSLATION_ACTIONS) => Promise<void>,
}>()

const emit = defineEmits(['toggle:action'])
const loading = ref(false)

const inProgress = computed(() => props.status === TRANSLATION_STATUS.INPROGRESS)

const removable = computed(() => {
    return (
        props.status == TRANSLATION_STATUS.COMPLETED ||
        props.status == TRANSLATION_STATUS.CANCELLED ||
        props.status == TRANSLATION_STATUS.FAILED ||
        props.status == TRANSLATION_STATUS.INTERRUPTED
    )
})

const executeAction = async (action: TRANSLATION_ACTIONS) => {
    loading.value = true
    await props.onAction(action);

    if (action == TRANSLATION_ACTIONS.RETRY) {
        // Special case to set loading to false when onAction is done
        // Retry is the only case that does not change props.status
        loading.value = false;
    }
}

watch(
    () => props.status,
    () => {
        loading.value = false
    }
)
</script>
