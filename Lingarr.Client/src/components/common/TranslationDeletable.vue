<template>
    <button
        v-if="
            (request &&
                request.status !== TRANSLATION_STATUS.COMPLETED &&
                request.status !== TRANSLATION_STATUS.CANCELLED) ||
            (item.status !== TRANSLATION_STATUS.COMPLETED &&
                item.status !== TRANSLATION_STATUS.CANCELLED)
        "
        :disabled="loading"
        @click="remove">
        <LoaderCircleIcon v-if="loading" class="h-5 w-5 animate-spin" />
        <TimesIcon v-else class="h-5 w-5 cursor-pointer" />
    </button>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import { IProgressMap, ITranslationRequest, TRANSLATION_STATUS } from '@/ts'
import TimesIcon from '@/components/icons/TimesIcon.vue'
import LoaderCircleIcon from '@/components/icons/LoaderCircleIcon.vue'

const { item, progressMap } = defineProps<{
    item: ITranslationRequest
    progressMap: IProgressMap
}>()

const emit = defineEmits(['toggle:remove'])

const loading = ref(false)
const request = computed(() => progressMap.get(item.id))

const remove = async () => {
    loading.value = true
    emit('toggle:remove')
}
</script>
