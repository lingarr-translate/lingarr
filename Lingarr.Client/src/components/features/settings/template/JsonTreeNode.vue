<template>
    <div class="group">
        <div
            class="hover:bg-accent/5 flex items-center gap-2 rounded px-1.5 py-1.5"
            :style="{ paddingLeft: `${depth * 1.25}rem` }">
            <CaretButton
                v-if="node.type === NODE_TYPE.OBJECT || node.type === NODE_TYPE.ARRAY"
                :is-expanded="!node.expanded"
                class="text-secondary-content/60 shrink-0 transition-colors duration-200 hover:text-primary-content"
                @click="toggleExpand" />
            <div v-else class="h-6 w-6 shrink-0" />

            <InputComponent
                v-if="parentType === 'object' || parentType === 'root'"
                :model-value="node.key"
                size="sm"
                :debounce="0"
                placeholder="key"
                class="w-28"
                @update:model-value="updateKey" />
            <span
                v-else-if="parentType === 'array'"
                class="bg-accent/20 rounded px-1.5 py-0.5 text-xs font-medium text-accent-content">
                [{{ arrayIndex }}]
            </span>

            <span class="text-secondary-content/60 text-xs">:</span>

            <SelectComponent
                :options="NODE_TYPE_OPTIONS"
                :selected="node.type"
                size="sm"
                placeholder="type"
                @update:selected="updateType($event as NodeType)" />

            <template v-if="node.type === NODE_TYPE.STRING">
                <InputComponent
                    :model-value="node.value as string"
                    size="sm"
                    :debounce="0"
                    placeholder="value"
                    class="min-w-0 flex-1"
                    @update:model-value="updateValue" />
            </template>
            <template v-else-if="node.type === NODE_TYPE.NUMBER">
                <InputComponent
                    :model-value="node.value as number"
                    :type="INPUT_TYPE.NUMBER"
                    size="sm"
                    :debounce="0"
                    class="w-24"
                    @update:model-value="(val: string) => updateValue(Number(val))" />
            </template>
            <template v-else-if="node.type === NODE_TYPE.BOOLEAN">
                <ToggleButton
                    :model-value="String(node.value)"
                    size="small"
                    @update:model-value="updateValue($event === 'true')" />
            </template>
            <template v-else-if="node.type === NODE_TYPE.OBJECT">
                <span class="text-secondary-content/60 text-xs">
                    { {{ node.children.length }}
                    {{ node.children.length === 1 ? 'property' : 'properties' }} }
                </span>
            </template>
            <template v-else-if="node.type === NODE_TYPE.ARRAY">
                <span class="text-secondary-content/60 text-xs">
                    [ {{ node.children.length }}
                    {{ node.children.length === 1 ? 'item' : 'items' }} ]
                </span>
            </template>

            <button
                class="text-secondary-content/40 ml-auto h-4 w-4 cursor-pointer shrink-0 opacity-0 transition-opacity hover:text-red-400 group-hover:opacity-100"
                title="Remove"
                @click="$emit('delete')">
                <TrashIcon class="h-4 w-4" />
            </button>
        </div>

        <div
            v-if="(node.type === NODE_TYPE.OBJECT || node.type === NODE_TYPE.ARRAY) && node.expanded"
            class="border-accent/15 ml-2 border-l"
            :style="{ marginLeft: `${depth * 1.25 + 0.5}rem` }">
            <JsonTreeNode
                v-for="(child, index) in node.children"
                :key="child.id"
                :node="child"
                :depth="depth + 1"
                :parent-type="node.type"
                :array-index="index"
                :next-id="nextId"
                @update="$emit('update')"
                @delete="removeChild(index)" />
            <ButtonComponent
                variant="ghost"
                size="xs"
                :style="{ marginLeft: `${(depth + 1) * 1.25}rem` }"
                @click="addChild">
                <PlusIcon class="mr-1 h-3 w-3" />
                {{ node.type === NODE_TYPE.OBJECT ? 'Add Property' : 'Add Item' }}
            </ButtonComponent>
        </div>
    </div>
</template>

<script setup lang="ts">
import { INPUT_TYPE, NODE_TYPE, NODE_TYPE_OPTIONS, TreeNode, NodeType } from '@/ts'
import CaretButton from '@/components/common/CaretButton.vue'
import ButtonComponent from '@/components/common/ButtonComponent.vue'
import InputComponent from '@/components/common/InputComponent.vue'
import SelectComponent from '@/components/common/SelectComponent.vue'
import TrashIcon from '@/components/icons/TrashIcon.vue'
import PlusIcon from '@/components/icons/PlusIcon.vue'
import ToggleButton from '@/components/common/ToggleButton.vue'

const {
    node,
    depth = 0,
    parentType = NODE_TYPE.ROOT,
    arrayIndex,
    nextId
} = defineProps<{
    node: TreeNode
    depth?: number
    parentType?: 'object' | 'array' | 'root'
    arrayIndex?: number
    nextId: () => number
}>()

const emit = defineEmits<{
    update: []
    delete: []
}>()

function toggleExpand() {
    node.expanded = !node.expanded
}

function updateKey(newKey: string) {
    node.key = newKey
    emit('update')
}

function updateValue(newValue: string | number | boolean) {
    node.value = newValue
    emit('update')
}

function updateType(newType: NodeType) {
    const oldType = node.type
    node.type = newType

    if (newType === NODE_TYPE.OBJECT || newType === NODE_TYPE.ARRAY) {
        if (oldType !== NODE_TYPE.OBJECT && oldType !== NODE_TYPE.ARRAY) {
            node.value = null
            node.children = []
            node.expanded = true
        }
    } else {
        node.children = []
        if (newType === NODE_TYPE.STRING) {
            node.value = ''
        } else if (newType === NODE_TYPE.NUMBER) {
            node.value = 0
        } else if (newType === NODE_TYPE.BOOLEAN) {
            node.value = false
        }
    }

    emit('update')
}

function addChild() {
    const newNode: TreeNode = {
        id: nextId(),
        key: '',
        type: NODE_TYPE.STRING,
        value: '',
        children: [],
        expanded: false
    }
    node.children.push(newNode)
    emit('update')
}

function removeChild(index: number) {
    node.children.splice(index, 1)
    emit('update')
}
</script>
