<template>
    <div ref="element" />
</template>

<script lang="ts" setup>
import { onBeforeUnmount, onMounted, ref } from 'vue'

interface props {
    options: object
}
const emit = defineEmits(['intersect'])
const props = defineProps<props>()
const element = ref()

const observer = new IntersectionObserver(([entry]) => {
    if (entry && entry.isIntersecting) {
        emit('intersect')
    }
}, props.options)

onBeforeUnmount(() => {
    observer.disconnect()
})
onMounted(() => {
    observer.observe(element.value)
})
</script>
