<template>
    <div class="cursor-pointer" @click="trigger">
        <LoaderCircleIcon v-if="loading" class="h-5 w-5 animate-spin" />
        <StartIcon v-else class="h-5 w-5" />
    </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import LoaderCircleIcon from '@/components/icons/LoaderCircleIcon.vue'
import StartIcon from '@/components/icons/StartIcon.vue'
import { delay } from '@/utils/delay'

const emit = defineEmits(['toggle:trigger'])
const loading = ref(false)

const trigger = async () => {
    if (loading.value) return
    loading.value = true
    emit('toggle:trigger')
    await delay(1000)
    loading.value = false
}
</script>
