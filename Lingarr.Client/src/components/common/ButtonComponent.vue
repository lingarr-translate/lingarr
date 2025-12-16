<template>
    <button
        :type="type"
        :disabled="disabled || loading"
        class="focus-visible:ring-accent focus-visible:ring-offset-primary inline-flex cursor-pointer items-center justify-center rounded-md px-6 py-2 text-sm font-semibold transition-all duration-200 focus-visible:ring-2 focus-visible:ring-offset-2 focus-visible:outline-none disabled:cursor-not-allowed disabled:opacity-50"
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
    size?: 'sm' | 'md' | 'lg'
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
            return 'bg-accent text-accent-content border border-accent hover:border-accent/80 hover:shadow-md'
        case 'secondary':
            return 'bg-secondary text-secondary-content border border-accent hover:border-accent/80 hover:shadow-sm'
        case 'accent':
            return 'bg-transparent text-accent-content border-2 border-accent hover:bg-accent/10 hover:border-accent/80 hover:shadow-md'
        case 'ghost':
            return 'bg-transparent text-accent-content hover:bg-accent/10 border border-transparent hover:border-accent/50'
        default:
            return 'bg-accent text-accent-content border border-accent hover:border-accent/80 hover:shadow-md'
    }
})

const sizeClasses = computed(() => {
    switch (props.size) {
        case 'sm':
            return 'px-4 py-1 text-xs'
        case 'md':
            return 'px-6 py-2 text-sm'
        case 'lg':
            return 'px-8 py-3 text-base'
        default:
            return 'px-6 py-2 text-sm'
    }
})

const handleClick = (event: MouseEvent) => {
    if (!props.disabled && !props.loading) {
        emit('click', event)
    }
}
</script>
