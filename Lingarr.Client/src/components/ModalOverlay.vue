<template>
    <div v-if="isOpen" class="fixed inset-0 z-50 flex items-center justify-center">
        <div class="absolute inset-0 bg-black opacity-80" @click="isOpen = !isOpen"></div>

        <div class="relative z-10 h-full overflow-y-auto rounded-md bg-neutral-800 p-6 shadow-md">
            <AutocompleteSelect :item="item" @select="onOptionSelected()" />
        </div>
    </div>
</template>

<script setup lang="ts">
import AutocompleteSelect from './AutocompleteSelect.vue'
import { IResource } from '@/ts/resource'

interface Props {
    item: IResource
}
defineProps<Props>()

const isOpen = defineModel()
const onOptionSelected = () => {
    isOpen.value = !isOpen.value
}
</script>
