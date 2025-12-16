<template>
    <div class="absolute inset-y-0 right-0 flex items-center">
        <span v-if="isValid" class="text-green-500">
            <CheckMarkCircleIcon
                :class="{
                    'h-6 w-6': size === 'lg',
                    'h-5 w-5': size === 'md',
                    'h-4 w-4': size === 'sm'
                }" />
        </span>
        <span v-if="isInvalid" class="text-red-500">
            <ExclamationIcon
                :class="{
                    'h-6 w-6': size === 'lg',
                    'h-5 w-5': size === 'md',
                    'h-4 w-4': size === 'sm'
                }" />
        </span>
    </div>
</template>

<script setup lang="ts">
import CheckMarkCircleIcon from '@/components/icons/CheckMarkCircleIcon.vue'
import ExclamationIcon from '@/components/icons/ExclamationIcon.vue'

withDefaults(
    defineProps<{
        isValid: boolean
        isInvalid: boolean
        size?: 'sm' | 'md' | 'lg'
    }>(),
    {
        size: 'md'
    }
)
</script>
