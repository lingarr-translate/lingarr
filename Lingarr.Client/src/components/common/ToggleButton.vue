<template>
    <label v-if="label" class="mb-1 block text-sm">
        {{ label }}
    </label>
    <div class="flex items-center space-x-3">
        <button
            type="button"
            role="switch"
            :aria-checked="isActive"
            :class="[
                'relative inline-flex shrink-0 cursor-pointer items-center border border-accent transition-colors duration-200 ease-in-out',
                isActive ? 'bg-accent/30' : '',
                size === 'small'
                    ? 'h-[1.17rem] w-[2.08rem] rounded-sm p-0.5'
                    : 'h-7 w-[3.125rem] rounded-md p-1'
            ]"
            @click="toggle">
            <span
                aria-hidden="true"
                :class="[
                    'pointer-events-none inline-block transform bg-accent shadow-sm ring-0 transition duration-200 ease-in-out',
                    size === 'small' ? 'h-[0.83rem] w-[0.83rem] rounded-sm' : 'h-5 w-5 rounded-md',
                    isActive
                        ? size === 'small'
                            ? 'translate-x-[0.83rem]'
                            : 'translate-x-5'
                        : 'translate-x-0'
                ]"></span>
        </button>
        <slot></slot>
    </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'

const {
    label,
    modelValue = 'false',
    size = 'default'
} = defineProps<{
    label?: string
    modelValue?: string | boolean
    size?: 'default' | 'small'
}>()

const isActive = computed(() => {
    return modelValue.toString() === 'true'
})

const emit = defineEmits<{
    (e: 'update:modelValue', value: string | boolean): void
    (e: 'toggle:update'): void
}>()

const toggle = () => {
    const current = modelValue.toString() === 'true'
    const newValue = current ? false : true
    emit('update:modelValue', typeof modelValue === 'boolean' ? newValue : String(newValue))
    emit('toggle:update')
}
</script>
