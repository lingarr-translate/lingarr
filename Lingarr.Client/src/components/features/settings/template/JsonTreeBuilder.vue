<template>
    <div class="border-accent/30 bg-primary/50 rounded-md border p-3">
        <div v-if="parseError" class="mb-3">
            <StatusMessage :message="parseError" type="error" />
        </div>
        <div v-else>
            <JsonTreeNode
                v-for="(node, index) in tree"
                :key="node.id"
                :node="node"
                :depth="0"
                parent-type="root"
                :array-index="index"
                :next-id="nextId"
                @update="onTreeChange"
                @delete="removeRoot(index)" />
            <ButtonComponent variant="ghost" size="xs" class="mt-2" @click="addRootProperty">
                <PlusIcon class="mr-1 h-3 w-3" />
                Add Property
            </ButtonComponent>
        </div>
    </div>
</template>

<script setup lang="ts">
import { useJsonTree } from '@/composables/useJsonTree'
import JsonTreeNode from './JsonTreeNode.vue'
import ButtonComponent from '@/components/common/ButtonComponent.vue'
import PlusIcon from '@/components/icons/PlusIcon.vue'
import StatusMessage from '@/components/common/StatusMessage.vue'

const { modelValue } = defineProps<{
    modelValue: string
}>()

const emit = defineEmits<{
    'update:modelValue': [value: string]
}>()

const {
    tree,
    parseError,
    nextId,
    onTreeChange,
    addRootProperty,
    removeRoot,
    watchModelValue
} = useJsonTree(emit)

watchModelValue(() => modelValue)
</script>
