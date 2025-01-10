<template>
    <button
        v-if="translationStatus == TRANSLATION_STATUS.INPROGRESS"
        :disabled="loading"
        @click="remove">
        <LoaderCircleIcon v-if="loading" class="h-5 w-5 animate-spin" />
        <TimesIcon v-else class="h-5 w-5 cursor-pointer" />
    </button>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { TRANSLATION_STATUS, TranslationStatus } from '@/ts'
import TimesIcon from '@/components/icons/TimesIcon.vue'
import LoaderCircleIcon from '@/components/icons/LoaderCircleIcon.vue'

const { translationStatus } = defineProps<{
    translationStatus: TranslationStatus
}>()

const emit = defineEmits(['toggle:remove'])
const loading = ref(false)

const remove = async () => {
    loading.value = true
    emit('toggle:remove')
}
</script>
