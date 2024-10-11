<template>
    <div class="absolute inset-y-0 right-0 flex items-center pr-3">
        <span v-if="isValid" class="text-green-500">
            <CheckMarkIcon class="h-5 w-5" />
        </span>
        <span v-if="isInvalid" class="text-red-500">
            <ExclamationIcon class="h-5 w-5" />
        </span>
    </div>
</template>

<script setup lang="ts">
import CheckMarkIcon from '@/components/icons/CheckMarkIcon.vue'
import ExclamationIcon from '@/components/icons/ExclamationIcon.vue'

defineProps<{
    isValid: boolean
    isInvalid: boolean
}>()
</script>
