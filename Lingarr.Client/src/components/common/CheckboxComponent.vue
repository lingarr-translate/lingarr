<template>
    <div class="inline-flex items-center">
        <input
            :id="id"
            type="checkbox"
            :checked="modelValue"
            :disabled="disabled"
            class="relative h-4 w-4 cursor-pointer appearance-none rounded border border-accent bg-secondary checked:bg-accent focus:outline-none focus:ring-1 focus:ring-accent focus:ring-offset-1 disabled:cursor-not-allowed disabled:opacity-50"
            @change="updateValue($event)" />
        <CheckMarkIcon
            class="pointer-events-none absolute h-4 w-4 fill-none text-primary-content"
            :class="{ hidden: !modelValue }" />
        <label v-if="label" :for="id" class="ml-2 cursor-pointer">{{ label }}</label>
    </div>
</template>

<script setup lang="ts">
import CheckMarkIcon from '@/components/icons/CheckMarkIcon.vue'

withDefaults(
    defineProps<{
        modelValue: boolean
        id?: string
        label?: string
        disabled?: boolean
    }>(),
    {
        label: '',
        id: `checkbox-${Math.random().toString(36).substring(2, 9)}`,
        disabled: false
    }
)

const emit = defineEmits(['update:modelValue', 'change'])

const updateValue = (event: Event) => {
    const target = event.target as HTMLInputElement
    emit('update:modelValue', target.checked)
    emit('change', target.checked)
}
</script>
