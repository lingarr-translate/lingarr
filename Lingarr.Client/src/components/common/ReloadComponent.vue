<template>
    <div title="Refresh" @click="reload">
        <ReloadIcon
            class="h-6 w-6 cursor-pointer transition-all duration-300 ease-in-out"
            :class="{ 'animate-spin': reloading }" />
    </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import ReloadIcon from '@/components/icons/ReloadIcon.vue'

const emit = defineEmits(['toggle:update'])
const reloading = ref(false)

function reload() {
    reloading.value = true
    emit('toggle:update', true)

    setTimeout(() => {
        reloading.value = false
    }, 1000)
}
</script>
