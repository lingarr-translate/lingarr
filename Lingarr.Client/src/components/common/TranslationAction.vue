<template>
    <div class="flex items-center gap-2">
        <button
            class="text-primary-content hover:text-primary-content/50 cursor-pointer transition-colors"
            @click="router.push({ name: 'translation-detail', params: { id: item.id } })">
            <EyeOnIcon class="h-5 w-5" />
        </button>
        <div class="flex w-[3rem] items-center gap-2">
            <LoaderCircleIcon v-if="loading" class="h-5 w-5 animate-spin" />
            <button
                v-else-if="inProgress"
                class="text-primary-content hover:text-primary-content/50 cursor-pointer transition-colors"
                @click="executeAction(TRANSLATION_ACTIONS.CANCEL)">
                <TimesIcon class="h-5 w-5" />
            </button>
            <template v-else-if="removable">
                <button
                    :disabled="loading"
                    class="text-primary-content hover:text-primary-content/50 cursor-pointer transition-colors"
                    @click="executeAction(TRANSLATION_ACTIONS.RETRY)">
                    <RetryIcon class="h-5 w-5" />
                </button>
                <button
                    :disabled="loading"
                    class="text-primary-content hover:text-primary-content/50 cursor-pointer transition-colors"
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
import TimesIcon from '@/components/icons/TimesIcon.vue'
import LoaderCircleIcon from '@/components/icons/LoaderCircleIcon.vue'
import TrashIcon from '@/components/icons/TrashIcon.vue'
import RetryIcon from '@/components/icons/RetryIcon.vue'
import EyeOnIcon from '@/components/icons/EyeOnIcon.vue'
const router = useRouter()

const props = defineProps<{
    item: ITranslationRequest
    onAction: (action: TRANSLATION_ACTIONS) => Promise<void>
}>()

const loading = ref(false)

const inProgress = computed(() =>
    props.item.status === TRANSLATION_STATUS.INPROGRESS ||
    props.item.status === TRANSLATION_STATUS.PENDING
)

const removable = computed(() =>
    props.item.status !== TRANSLATION_STATUS.PENDING &&
    props.item.status !== TRANSLATION_STATUS.INPROGRESS
)

const executeAction = async (action: TRANSLATION_ACTIONS) => {
    loading.value = true
    await props.onAction(action)

    if (action == TRANSLATION_ACTIONS.RETRY) {
        // Special case to set loading to false when onAction is done
        // Retry is the only case that does not change props.status
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
