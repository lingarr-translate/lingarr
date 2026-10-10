<template>
    <button
        :type="type"
        :disabled="disabled || loading"
        :aria-busy="loading"
        class="focus-visible:ring-accent focus-visible:ring-offset-primary inline-flex cursor-pointer items-center justify-center rounded-lg text-sm font-semibold transition-colors duration-200 focus-visible:ring-2 focus-visible:ring-offset-2 focus-visible:outline-none disabled:cursor-not-allowed disabled:opacity-50"
        :class="[variantClasses, sizeClasses]"
        @click="handleClick">
        <LoaderCircleIcon v-if="loading" class="mr-2 h-4 w-4 animate-spin" />
        <slot></slot>
    </button>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import LoaderCircleIcon from '@/components/icons/LoaderCircleIcon.vue'

interface Props {
    variant?: 'primary' | 'secondary' | 'accent' | 'ghost'
    size?: 'xs' | 'sm' | 'md' | 'lg'
    type?: 'button' | 'submit' | 'reset'
    disabled?: boolean
    loading?: boolean
}

const props = withDefaults(defineProps<Props>(), {
    variant: 'primary',
    size: 'md',
    type: 'button',
    disabled: false,
    loading: false
})

const emit = defineEmits<{
    click: [event: MouseEvent]
}>()

const variantClasses = computed(() => {
    switch (props.variant) {
        case 'primary':
            return 'ui-button-raised bg-accent text-accent-content border border-accent hover:border-accent/80 hover:shadow-md'
        case 'secondary':
            return 'ui-button-raised bg-secondary text-secondary-content border ui-border hover:border-accent/80 hover:shadow-sm'
        case 'accent':
            return 'bg-transparent text-primary-content border border-accent/60 hover:bg-accent/10 hover:border-accent/80 hover:shadow-md'
        case 'ghost':
            return 'bg-transparent text-primary-content hover:bg-accent/10 border border-transparent hover:border-accent/50'
        default:
            return 'ui-button-raised bg-accent text-accent-content border border-accent hover:border-accent/80 hover:shadow-md'
    }
})

const sizeClasses = computed(() => {
    switch (props.size) {
        case 'xs':
            return 'min-h-9 px-3 py-1.5 text-xs'
        case 'sm':
            return 'min-h-11 px-4 py-2 text-sm'
        case 'md':
            return 'min-h-11 px-5 py-2 text-sm'
        case 'lg':
            return 'min-h-12 px-6 py-3 text-base'
        default:
            return 'min-h-11 px-5 py-2 text-sm'
    }
})

const handleClick = (event: MouseEvent) => {
    if (!props.disabled && !props.loading) {
        emit('click', event)
    }
}
</script>
