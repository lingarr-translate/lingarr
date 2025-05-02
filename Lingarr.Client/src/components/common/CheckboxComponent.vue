<template>
    <div class="inline-flex items-center">
        <input
            :id="id"
            type="checkbox"
            :checked="modelValue"
            :disabled="disabled"
            class="border-accent bg-secondary checked:bg-accent focus:ring-accent relative h-4 w-4 cursor-pointer appearance-none rounded border focus:ring-1 focus:ring-offset-1 focus:outline-none disabled:cursor-not-allowed disabled:opacity-50"
            @change="updateValue($event)" />
        <CheckMarkIcon
            class="text-primary-content pointer-events-none absolute h-4 w-4 fill-none"
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
