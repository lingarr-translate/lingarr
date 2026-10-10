<template>
    <div class="flex min-h-11 items-center gap-3">
        <button
            :id="switchId"
            type="button"
            role="switch"
            :aria-checked="isActive"
            :aria-label="ariaLabel"
            :aria-labelledby="
                !ariaLabel && (label || $slots.default) ? `${switchId}-label` : undefined
            "
            class="group inline-flex min-h-11 min-w-11 shrink-0 cursor-pointer items-center rounded-lg"
            @click="toggle">
            <span
                :class="[
                    isActive ? 'border-accent bg-accent' : 'ui-border bg-primary',
                    size === 'small' ? 'h-6 w-10' : 'h-7 w-12'
                ]"
                class="relative inline-flex items-center rounded-full border-2 transition-colors">
                <span
                    aria-hidden="true"
                    :class="[
                        isActive ? 'bg-accent-content' : 'bg-primary-content',
                        size === 'small' ? 'h-4 w-4' : 'h-5 w-5',
                        isActive
                            ? size === 'small'
                                ? 'translate-x-[18px]'
                                : 'translate-x-[22px]'
                            : 'translate-x-0.5'
                    ]"
                    class="rounded-full shadow-sm transition-transform"></span>
            </span>
        </button>
        <label
            v-if="label || $slots.default"
            :id="`${switchId}-label`"
            :for="switchId"
            class="cursor-pointer text-sm leading-relaxed">
            <template v-if="label">{{ label }}</template>
            <slot></slot>
        </label>
    </div>
</template>

<script setup lang="ts">
import { computed, useId } from 'vue'

const {
    label,
    ariaLabel,
    modelValue = 'false',
    size = 'default'
} = defineProps<{
    label?: string
    ariaLabel?: string
    modelValue?: string | boolean
    size?: 'default' | 'small'
}>()

const switchId = useId()
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
