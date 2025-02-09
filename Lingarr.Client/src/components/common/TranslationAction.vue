<template>
    <button :disabled="loading" @click="remove">
        <LoaderCircleIcon v-if="loading" class="h-5 w-5 animate-spin" />
        <TimesIcon
            v-else-if="translationStatus == TRANSLATION_STATUS.INPROGRESS"
            class="h-5 w-5 cursor-pointer" />
        <TrashIcon
            v-else-if="
                translationStatus == TRANSLATION_STATUS.COMPLETED ||
                translationStatus == TRANSLATION_STATUS.CANCELLED
            "
            class="h-5 w-5 cursor-pointer" />
    </button>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { TRANSLATION_STATUS, TranslationStatus } from '@/ts'
import TimesIcon from '@/components/icons/TimesIcon.vue'
import LoaderCircleIcon from '@/components/icons/LoaderCircleIcon.vue'
import TrashIcon from '@/components/icons/TrashIcon.vue'

const { translationStatus } = defineProps<{
    translationStatus: TranslationStatus
}>()

const emit = defineEmits(['toggle:action'])
const loading = ref(false)

const remove = async () => {
    loading.value = true
    let action = ''

    if (translationStatus === TRANSLATION_STATUS.INPROGRESS) {
        action = 'cancel'
    } else if (
        translationStatus === TRANSLATION_STATUS.COMPLETED ||
        translationStatus === TRANSLATION_STATUS.CANCELLED
    ) {
        action = 'remove'
    }
    emit('toggle:action', action)
}
</script>
