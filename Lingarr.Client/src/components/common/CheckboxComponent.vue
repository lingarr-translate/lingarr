<template>
    <div class="relative inline-flex min-h-11 min-w-11 items-center justify-center">
        <label
            :for="checkboxId"
            class="relative inline-flex h-11 w-11 shrink-0 cursor-pointer items-center justify-center">
            <input
                :id="checkboxId"
                :aria-label="ariaLabel || (!label ? 'Select item' : undefined)"
                type="checkbox"
                :checked="modelValue"
                :disabled="disabled"
                class="border-accent bg-secondary checked:bg-accent focus:ring-accent relative h-5 w-5 cursor-pointer appearance-none rounded border focus:ring-1 focus:ring-offset-1 focus:outline-none disabled:cursor-not-allowed disabled:opacity-50"
                @change="updateValue($event)" />
            <CheckMarkIcon
                class="text-accent-content pointer-events-none absolute h-5 w-5 fill-none"
                :class="{ hidden: !modelValue }" />
        </label>
        <label v-if="label" :for="checkboxId" class="ml-2 cursor-pointer">{{ label }}</label>
    </div>
</template>

<script setup lang="ts">
import { computed, useId } from 'vue'
import CheckMarkIcon from '@/components/icons/CheckMarkIcon.vue'

const props = withDefaults(
    defineProps<{
        modelValue: boolean
        id?: string
        label?: string
        ariaLabel?: string
        disabled?: boolean
    }>(),
    {
        label: '',
        disabled: false
    }
)

const generatedId = useId()
const checkboxId = computed(() => props.id || generatedId)

const emit = defineEmits(['update:modelValue', 'change'])

const updateValue = (event: Event) => {
    const target = event.target as HTMLInputElement
    emit('update:modelValue', target.checked)
    emit('change', target.checked)
}
</script>
