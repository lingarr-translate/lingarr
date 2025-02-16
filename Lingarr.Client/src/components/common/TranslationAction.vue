<template>
    <button :disabled="loading" @click="remove">
        <LoaderCircleIcon v-if="loading" class="h-5 w-5 animate-spin" />
        <TimesIcon v-else-if="inProgress" class="h-5 w-5 cursor-pointer" />
        <TrashIcon v-else-if="removable" class="h-5 w-5 cursor-pointer" />
    </button>
</template>

<script setup lang="ts">
import { ref, computed, watch } from 'vue'
import { TRANSLATION_STATUS, TranslationStatus } from '@/ts'
import TimesIcon from '@/components/icons/TimesIcon.vue'
import LoaderCircleIcon from '@/components/icons/LoaderCircleIcon.vue'
import TrashIcon from '@/components/icons/TrashIcon.vue'

const props = defineProps<{
    status: TranslationStatus
}>()

const emit = defineEmits(['toggle:action'])
const loading = ref(false)

const inProgress = computed(() => props.status === TRANSLATION_STATUS.INPROGRESS)

const removable = computed(() => {
    return (
        props.status == TRANSLATION_STATUS.COMPLETED ||
        props.status == TRANSLATION_STATUS.CANCELLED ||
        props.status == TRANSLATION_STATUS.FAILED
    )
})

const action = computed(() => (inProgress.value ? 'cancel' : removable.value ? 'remove' : ''))

const remove = async () => {
    loading.value = true
    emit('toggle:action', action.value)
}

watch(
    () => props.status,
    () => {
        loading.value = false
    }
)
</script>
